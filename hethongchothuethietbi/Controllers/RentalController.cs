using hethongchothuethietbi.Data;
using hethongchothuethietbi.Extensions;
using hethongchothuethietbi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hethongchothuethietbi.Controllers
{
    [Authorize]
    public class RentalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public RentalController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Checkout()
        {
            var cartItems = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cartItems.Any())
                return RedirectToAction("Index", "Equipment");

            var equipmentIds = cartItems.Select(c => c.EquipmentId).ToList();
            var equipments = await _context.Equipments
                .Where(e => equipmentIds.Contains(e.Id))
                .ToListAsync();

            var model = new CheckoutViewModel
            {
                Items = cartItems.Select(c =>
                {
                    var eq = equipments.FirstOrDefault(e => e.Id == c.EquipmentId);
                    return new CheckoutItemViewModel
                    {
                        EquipmentId = c.EquipmentId,
                        EquipmentName = eq?.Name ?? "",
                        PricePerDay = eq?.PricePerDay ?? 0,
                        Quantity = c.Quantity
                    };
                }).ToList(),
                ExpectedPickUpTime = DateTime.Now.AddHours(1),
                ExpectedReturnTime = DateTime.Now.AddDays(1).AddHours(1)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var cartItems = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();

            // Load equipments từ DB
            var equipmentIds = cartItems.Select(c => c.EquipmentId).ToList();
            var equipments = new List<Equipment>();
            if (equipmentIds.Any())
            {
                equipments = await _context.Equipments
                    .Where(e => equipmentIds.Contains(e.Id))
                    .ToListAsync();
            }

            // Rebuild model từ cart & database
            if (cartItems.Any())
            {
                model.Items = cartItems.Select(c =>
                {
                    var eq = equipments.FirstOrDefault(e => e.Id == c.EquipmentId);
                    return new CheckoutItemViewModel
                    {
                        EquipmentId = c.EquipmentId,
                        EquipmentName = eq?.Name ?? "",
                        PricePerDay = eq?.PricePerDay ?? 0,
                        Quantity = c.Quantity
                    };
                }).ToList();
            }

            // Validation: 24 hours minimum
            if ((model.ExpectedReturnTime - model.ExpectedPickUpTime).TotalHours < 24)
            {
                ModelState.AddModelError("", "Thời gian thuê phải tối thiểu 24 giờ!");
                return View(model);
            }

            if (!cartItems.Any())
                return RedirectToAction("Index", "Equipment");

            // Kiểm tra số lượng & trạng thái (Available)
            foreach (var item in cartItems)
            {
                var equipment = equipments.FirstOrDefault(e => e.Id == item.EquipmentId);
                if (equipment == null || equipment.Quantity < item.Quantity || equipment.Status != EquipmentStatus.Available)
                {
                    ModelState.AddModelError("", $"Thiết bị '{equipment?.Name}' không đủ số lượng hoặc đã hết!");
                    return View(model);
                }
            }

            // Tính tiền
            decimal totalAmount = 0;
            foreach (var item in cartItems)
            {
                var equipment = equipments.FirstOrDefault(e => e.Id == item.EquipmentId);
                if (equipment != null)
                {
                    var days = (decimal)Math.Ceiling((model.ExpectedReturnTime - model.ExpectedPickUpTime).TotalDays);
                    totalAmount += equipment.PricePerDay * days * item.Quantity;
                }
            }

            // Tạo RentalOrder
            var order = new RentalOrder
            {
                CustomerId = user.Id,
                TotalAmount = totalAmount,
                DepositAmount = totalAmount * 0.3m,
                OrderStatus = RentalOrderStatus.PendingPayment,
                ExpectedPickUpTime = model.ExpectedPickUpTime,
                ExpectedReturnTime = model.ExpectedReturnTime
            };

            _context.RentalOrders.Add(order);
            await _context.SaveChangesAsync();

            // Tạo OrderDetail và cập nhật kho
            foreach (var item in cartItems)
            {
                var equipment = equipments.FirstOrDefault(e => e.Id == item.EquipmentId);
                if (equipment != null)
                {
                    var days = (decimal)Math.Ceiling((model.ExpectedReturnTime - model.ExpectedPickUpTime).TotalDays);

                    _context.RentalOrderDetails.Add(new RentalOrderDetail
                    {
                        OrderId = order.Id,
                        EquipmentId = item.EquipmentId,
                        UnitPriceAtBooking = equipment.PricePerDay * days,
                        Quantity = item.Quantity
                    });

                    // Giảm số lượng trong kho khi đặt đơn
                    equipment.Quantity -= item.Quantity;
                    if (equipment.Quantity <= 0)
                    {
                        equipment.Status = EquipmentStatus.Rented;
                        equipment.Quantity = 0;
                    }
                    _context.Equipments.Update(equipment);
                }
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove("Cart");

            return RedirectToAction("OrderDetail", new { id = order.Id });
        }

        public async Task<IActionResult> MyOrders()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var orders = await _context.RentalOrders
                .Where(o => o.CustomerId == user.Id && !o.IsDeleted)
                .Include(o => o.Details)
                .ThenInclude(d => d.Equipment)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> OrderDetail(int id)
        {
            var order = await _context.RentalOrders
                .Include(o => o.Details)
                .ThenInclude(d => d.Equipment)
                .Include(o => o.Comments)
                .ThenInclude(c => c.Author)
                .Include(o => o.Messages)
                .ThenInclude(m => m.Sender)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user != null && order.CustomerId != user.Id && !User.IsInRole("Admin"))
                return Forbid();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int orderId, string content, IFormFile image)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var order = await _context.RentalOrders.FindAsync(orderId);
            if (order == null || order.CustomerId != user.Id)
                return Forbid();

            string imageUrl = null;
            if (image != null && image.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{image.FileName}";
                var path = Path.Combine("wwwroot/uploads", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                imageUrl = $"/uploads/{fileName}";
            }

            var comment = new OrderComment
            {
                OrderId = orderId,
                AuthorId = user.Id,
                Content = content,
                ImageUrl = imageUrl ?? ""
            };

            _context.OrderComments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToAction("OrderDetail", new { id = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPaymentProof(int orderId, IFormFile proofImage)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var order = await _context.RentalOrders.FindAsync(orderId);
            if (order == null || order.CustomerId != user.Id)
                return Forbid();

            if (order.OrderStatus != RentalOrderStatus.PendingPayment)
            {
                TempData["Error"] = "Chỉ có thể gửi biên lai cho đơn chờ thanh toán!";
                return RedirectToAction("OrderDetail", new { id = orderId });
            }

            if (proofImage == null || proofImage.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn hình ảnh biên lai!";
                return RedirectToAction("OrderDetail", new { id = orderId });
            }

            var fileName = $"{Guid.NewGuid()}_{proofImage.FileName}";
            var uploadsFolder = Path.Combine("wwwroot/uploads", "payment-proofs");
            Directory.CreateDirectory(uploadsFolder);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await proofImage.CopyToAsync(stream);
            }

            var comment = new OrderComment
            {
                OrderId = orderId,
                AuthorId = user.Id,
                Content = "✅ Biên lai thanh toán cọc",
                ImageUrl = $"/uploads/payment-proofs/{fileName}",
                CreatedDate = DateTime.UtcNow
            };

            _context.OrderComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Gửi biên lai thành công! Chờ nhân viên xác nhận.";
            return RedirectToAction("OrderDetail", new { id = orderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var order = await _context.RentalOrders
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            if (order.CustomerId != user.Id)
                return Forbid();

            if (order.OrderStatus == RentalOrderStatus.Rented || order.OrderStatus == RentalOrderStatus.Completed)
            {
                TempData["Error"] = "Không thể hủy đơn đang thuê hoặc đã hoàn thành!";
                return RedirectToAction("OrderDetail", new { id });
            }

            order.OrderStatus = RentalOrderStatus.Cancelled;

            // Restore quantity
            foreach (var detail in order.Details)
            {
                var equipment = await _context.Equipments.FindAsync(detail.EquipmentId);
                if (equipment != null)
                {
                    equipment.Quantity += detail.Quantity;
                    if (equipment.Status == EquipmentStatus.Rented && equipment.Quantity > 0)
                    {
                        equipment.Status = EquipmentStatus.Available;
                    }
                    _context.Equipments.Update(equipment);
                }
            }

            _context.RentalOrders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Hủy đơn hàng thành công!";
            return RedirectToAction("MyOrders");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var order = await _context.RentalOrders.FindAsync(id);
            if (order == null)
                return NotFound();

            if (order.CustomerId != user.Id)
                return Forbid();

            if (order.OrderStatus == RentalOrderStatus.Rented)
            {
                TempData["Error"] = "Không thể xóa đơn đang thuê!";
                return RedirectToAction("OrderDetail", new { id });
            }

            order.IsDeleted = true;
            _context.RentalOrders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa đơn hàng thành công!";
            return RedirectToAction("MyOrders");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDeleteOrder(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var order = await _context.RentalOrders.FindAsync(id);
            if (order == null)
                return NotFound();

            if (order.CustomerId != user.Id)
                return Forbid();

            if (order.OrderStatus != RentalOrderStatus.Completed)
            {
                TempData["Error"] = "Chỉ có thể ẩn đơn đã hoàn tất!";
                return RedirectToAction("OrderDetail", new { id });
            }

            order.IsDeleted = true;
            _context.RentalOrders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đơn hàng đã ẩn khỏi lịch sử!";
            return RedirectToAction("MyOrders");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int id, string messageContent)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var order = await _context.RentalOrders.FindAsync(id);
            if (order == null)
                return NotFound();

            if (order.CustomerId != user.Id)
                return Forbid();

            if (string.IsNullOrWhiteSpace(messageContent))
            {
                TempData["Error"] = "Nội dung tin nhắn không được để trống!";
                return RedirectToAction("OrderDetail", new { id });
            }

            var message = new OrderMessage
            {
                OrderId = id,
                SenderId = user.Id,
                Content = messageContent.Trim(),
                CreatedDate = DateTime.UtcNow
            };

            _context.OrderMessages.Add(message);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Gửi tin nhắn thành công!";
            return RedirectToAction("OrderDetail", new { id });
        }
    }
}

