using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hethongchothuethietbi.Data;
using hethongchothuethietbi.Models;
using Microsoft.EntityFrameworkCore;

namespace hethongchothuethietbi.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public OrderController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        public async Task<IActionResult> Index(string orderStatus = null)
        {
            ViewBag.Title = "Quản lý đơn hàng";

            var orders = _context.RentalOrders
                .Include(o => o.Customer)
                .Include(o => o.Details)
                .OrderByDescending(o => o.CreatedDate);

            if (!string.IsNullOrEmpty(orderStatus))
            {
                if (Enum.TryParse<RentalOrderStatus>(orderStatus, out var status))
                {
                    var filteredOrders = await orders.Where(o => o.OrderStatus == status).ToListAsync();
                    ViewBag.SelectedStatus = orderStatus;
                    return View(filteredOrders);
                }
            }

            return View(await orders.ToListAsync());
        }

        public async Task<IActionResult> Detail(int id)
        {
            var order = await _context.RentalOrders
                .Include(o => o.Customer)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Equipment)
                .Include(o => o.Comments)
                    .ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            ViewBag.Title = $"Chi tiết đơn hàng #{order.Id}";
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UploadQRCode(int id, IFormFile qrImage)
        {
            var order = await _context.RentalOrders.FindAsync(id);
            if (order == null)
                return NotFound();

            if (order.OrderStatus != RentalOrderStatus.PendingPayment)
            {
                TempData["Error"] = "Chỉ có thể upload mã QR cho đơn chờ thanh toán!";
                return RedirectToAction(nameof(Detail), new { id });
            }

            if (qrImage == null || qrImage.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn hình ảnh mã QR!";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var imagePath = await SaveUploadedImage(qrImage, "qr-codes");
            var staffId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var comment = new OrderComment
            {
                OrderId = id,
                AuthorId = staffId,
                Content = "📱 Mã QR thanh toán cọc",
                ImageUrl = imagePath,
                CreatedDate = DateTime.UtcNow
            };

            _context.OrderComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Upload mã QR thành công! Chờ khách hàng gửi biên lai.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UploadPaymentProof(int id, IFormFile proofImage)
        {
            var order = await _context.RentalOrders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (order.CustomerId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (order.OrderStatus != RentalOrderStatus.PendingPayment)
            {
                TempData["Error"] = "Chỉ có thể gửi biên lai cho đơn chờ thanh toán!";
                return RedirectToAction(nameof(Detail), new { id });
            }

            if (proofImage == null || proofImage.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn hình ảnh biên lai thanh toán!";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var imagePath = await SaveUploadedImage(proofImage, "payment-proofs");
            var comment = new OrderComment
            {
                OrderId = id,
                AuthorId = userId,
                Content = "✅ Biên lai thanh toán cọc",
                ImageUrl = imagePath,
                CreatedDate = DateTime.UtcNow
            };

            _context.OrderComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Gửi biên lai thành công! Chờ nhân viên xác nhận.";
            return RedirectToAction("OrderDetail", "Rental", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var order = await _context.RentalOrders
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            if (order.OrderStatus != RentalOrderStatus.PendingPayment)
            {
                TempData["Error"] = "Đơn hàng không ở trạng thái chờ xác nhận!";
                return RedirectToAction(nameof(Detail), new { id });
            }

            order.OrderStatus = RentalOrderStatus.Deposited;
            _context.RentalOrders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xác nhận thanh toán thành công! Bàn giao thiết bị.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> ApproveDeposit(int id)
        {
            var order = await _context.RentalOrders
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
                return NotFound();

            if (order.OrderStatus != RentalOrderStatus.PendingPayment)
            {
                TempData["Error"] = "Chỉ có thể phê duyệt đơn đang chờ thanh toán!";
                return RedirectToAction(nameof(Detail), new { id });
            }

            order.OrderStatus = RentalOrderStatus.Deposited;

            // Cập nhật trạng thái thiết bị khi phê duyệt cọc
            foreach (var detail in order.Details)
            {
                var equipment = await _context.Equipments.FindAsync(detail.EquipmentId);
                if (equipment != null)
                {
                    // Nếu số lượng > 0 thì đánh dấu một chiếc là đang sử dụng
                    if (equipment.Quantity > 0)
                    {
                        equipment.Quantity -= 1;
                    }
                    if (equipment.Quantity <= 0)
                    {
                        equipment.Status = EquipmentStatus.Rented;
                    }
                    _context.Equipments.Update(equipment);
                }
            }

            _context.RentalOrders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Phê duyệt đơn cọc thành công!";
            return RedirectToAction(nameof(Detail), new { id });
        }

        public async Task<IActionResult> Handover(int id)
        {
            var order = await _context.RentalOrders
                .Include(o => o.Customer)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Equipment)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            if (order.OrderStatus != RentalOrderStatus.Deposited)
            {
                TempData["Error"] = "Chỉ có thể bàn giao khi đơn hàng đã được phê duyệt cọc!";
                return RedirectToAction(nameof(Detail), new { id });
            }

            ViewBag.Title = $"Bàn giao thiết bị - Đơn #{order.Id}";
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmHandover(int id, string cccdVerified)
        {
            var order = await _context.RentalOrders
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            if (string.IsNullOrEmpty(cccdVerified))
            {
                TempData["Error"] = "Vui lòng xác nhận thông tin CCCD!";
                return RedirectToAction(nameof(Handover), new { id });
            }

            order.OrderStatus = RentalOrderStatus.Rented;

            foreach (var detail in order.Details)
            {
                var equipment = await _context.Equipments.FindAsync(detail.EquipmentId);
                if (equipment != null)
                {
                    equipment.Status = EquipmentStatus.Rented;
                    _context.Equipments.Update(equipment);
                }
            }

            _context.RentalOrders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Bàn giao thành công! Thiết bị đã được cập nhật trạng thái.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        public async Task<IActionResult> CheckIn(int id)
        {
            var order = await _context.RentalOrders
                .Include(o => o.Customer)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Equipment)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            if (order.OrderStatus != RentalOrderStatus.Rented)
            {
                TempData["Error"] = "Chỉ có thể nhận lại thiết bị từ đơn hàng đang thuê!";
                return RedirectToAction(nameof(Detail), new { id });
            }

            // Calculate late fee
            var lateFee = 0m;
            if (DateTime.Now > order.ExpectedReturnTime)
            {
                var lateHours = Math.Ceiling((DateTime.Now - order.ExpectedReturnTime).TotalHours);
                lateFee = (decimal)lateHours * (order.TotalAmount / (decimal)(order.ExpectedReturnTime - order.ExpectedPickUpTime).TotalHours);
            }

            ViewBag.LateFee = lateFee;
            ViewBag.Title = $"Nhận lại thiết bị - Đơn #{order.Id}";
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmCheckIn(int id, decimal lateFee)
        {
            var order = await _context.RentalOrders
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            order.OrderStatus = RentalOrderStatus.Completed;
            order.ActualReturnTime = DateTime.Now;
            order.PenaltyAmount = lateFee;

            foreach (var detail in order.Details)
            {
                var equipment = await _context.Equipments.FindAsync(detail.EquipmentId);
                if (equipment != null)
                {
                    // Restore equipment quantity and status
                    equipment.Quantity += detail.Quantity;
                    equipment.Status = EquipmentStatus.Available;
                    _context.Equipments.Update(equipment);
                }
            }

            _context.RentalOrders.Update(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Nhận lại thiết bị thành công! Đơn hàng đã hoàn tất.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        private async Task<string> SaveUploadedImage(IFormFile file, string folderName)
        {
            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", folderName);
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folderName}/{fileName}";
        }

        [HttpPost]
        public async Task<IActionResult> RejectOrder(int id)
        {
            var order = await _context.RentalOrders
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            if (order.OrderStatus != RentalOrderStatus.PendingPayment)
            {
                TempData["Error"] = "Chỉ có thể từ chối đơn chờ thanh toán!";
                return RedirectToAction(nameof(Detail), new { id });
            }

            order.OrderStatus = RentalOrderStatus.Cancelled;

            // Restore equipment quantity
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

            TempData["Success"] = "Đã từ chối đơn hàng!";
            return RedirectToAction(nameof(Detail), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.RentalOrders.FindAsync(id);
            if (order == null)
                return NotFound();

            if (order.OrderStatus != RentalOrderStatus.Completed)
            {
                TempData["Error"] = "Chỉ có thể xóa đơn hàng đã hoàn thành!";
                return RedirectToAction("Index");
            }

            _context.RentalOrders.Remove(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Xóa đơn hàng thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
