using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<AppUser> _userManager;

        public OrderController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment, UserManager<AppUser> userManager)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _userManager = userManager;
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
                .Include(o => o.Messages)
                    .ThenInclude(m => m.Sender)
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

        public async Task<IActionResult> ExportInvoice(int id)
        {
            var order = await _context.RentalOrders
                .Include(o => o.Customer)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Equipment)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            // Cho phép in hoá đơn ở trạng thái Completed (hoá đơn thanh toán)
            if (order.OrderStatus != RentalOrderStatus.Completed)
            {
                TempData["Error"] = "Chỉ có thể in hoá đơn cho đơn hàng đã hoàn tất!";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var html = GenerateInvoiceHtml(order);
            var pdf = System.Text.Encoding.UTF8.GetBytes(html);

            return File(pdf, "application/octet-stream", $"Invoice_{order.Id}_{DateTime.Now:yyyyMMddHHmmss}.html");
        }

        public async Task<IActionResult> ExportHandoverInvoice(int id)
        {
            var order = await _context.RentalOrders
                .Include(o => o.Customer)
                .Include(o => o.Details)
                    .ThenInclude(d => d.Equipment)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            // Cho phép in hoá đơn bàn giao ở trạng thái Rented (hoá đơn bàn giao)
            if (order.OrderStatus != RentalOrderStatus.Rented)
            {
                TempData["Error"] = "Chỉ có thể in hoá đơn bàn giao cho đơn hàng đang thuê!";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var html = GenerateHandoverInvoiceHtml(order);
            var pdf = System.Text.Encoding.UTF8.GetBytes(html);

            return File(pdf, "application/octet-stream", $"Handover_{order.Id}_{DateTime.Now:yyyyMMddHHmmss}.html");
        }

        private string GenerateInvoiceHtml(RentalOrder order)
        {
            var detailsHtml = string.Join("", order.Details.Select((d, i) => 
                $@"<tr>
                    <td>{i + 1}</td>
                    <td>{d.Equipment?.Name}</td>
                    <td>{d.Quantity}</td>
                    <td>{d.UnitPriceAtBooking:N0} VND</td>
                    <td>{(d.Quantity * d.UnitPriceAtBooking):N0} VND</td>
                </tr>"));

            var rentalDays = (order.ExpectedReturnTime - order.ExpectedPickUpTime).TotalDays;

            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <title>Hoá đơn - Đơn #{order.Id}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 15px; }}
        .title {{ font-size: 22px; font-weight: bold; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 10px; text-align: left; }}
        th {{ background: #f0f0f0; font-weight: bold; }}
        .total-section {{ text-align: right; margin-top: 20px; }}
        .total-line {{ font-size: 16px; font-weight: bold; margin-top: 10px; }}
        .footer {{ text-align: center; margin-top: 40px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='title'>🧾 HOÁ ĐƠN CHO THUÊ THIẾT BỊ</div>
        <div style='margin-top: 10px; color: #666;'>Đơn hàng #{order.Id}</div>
    </div>

    <table>
        <tr><td style='font-weight: bold;'>Khách hàng:</td><td>{order.Customer?.UserName}</td></tr>
        <tr><td style='font-weight: bold;'>Địa chỉ:</td><td>{order.Customer?.Address}</td></tr>
        <tr><td style='font-weight: bold;'>CCCD:</td><td>{order.Customer?.CccdNumber}</td></tr>
        <tr><td style='font-weight: bold;'>Ngày nhận:</td><td>{order.ExpectedPickUpTime:dd/MM/yyyy HH:mm}</td></tr>
        <tr><td style='font-weight: bold;'>Ngày trả:</td><td>{order.ExpectedReturnTime:dd/MM/yyyy HH:mm}</td></tr>
        <tr><td style='font-weight: bold;'>Thực tế trả:</td><td>{order.ActualReturnTime?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}</td></tr>
    </table>

    <h3>Chi tiết thiết bị:</h3>
    <table>
        <thead>
            <tr>
                <th>STT</th>
                <th>Tên thiết bị</th>
                <th>Số lượng</th>
                <th>Giá/ngày</th>
                <th>Thành tiền</th>
            </tr>
        </thead>
        <tbody>
            {detailsHtml}
        </tbody>
    </table>

    <div class='total-section'>
        <div>Số ngày thuê: <strong>{rentalDays:F0} ngày</strong></div>
        <div class='total-line'>Tổng tiền thuê: {order.TotalAmount:N0} VND</div>
        <div>Tiền cọc: {order.DepositAmount:N0} VND</div>
        <div>Phạt trễ hạn: {order.PenaltyAmount:N0} VND</div>
        <div class='total-line'>Cần thanh toán: {(order.TotalAmount + order.PenaltyAmount - order.DepositAmount):N0} VND</div>
    </div>

    <div class='footer'>
        <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
        <p>In ngày: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
    </div>
</body>
</html>";
        }

        private string GenerateHandoverInvoiceHtml(RentalOrder order)
        {
            var detailsHtml = string.Join("", order.Details.Select((d, i) => 
                $@"<tr>
                    <td>{i + 1}</td>
                    <td>{d.Equipment?.Name}</td>
                    <td>{d.Quantity}</td>
                    <td>{d.UnitPriceAtBooking:N0} VND</td>
                    <td>{(d.Quantity * d.UnitPriceAtBooking):N0} VND</td>
                </tr>"));

            var rentalDays = (order.ExpectedReturnTime - order.ExpectedPickUpTime).TotalDays;

            return $@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='UTF-8'>
    <title>Hoá Đơn Bàn Giao - Đơn #{order.Id}</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        .header {{ text-align: center; margin-bottom: 30px; border-bottom: 2px solid #f39c12; padding-bottom: 15px; }}
        .title {{ font-size: 22px; font-weight: bold; color: #f39c12; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th, td {{ border: 1px solid #ddd; padding: 10px; text-align: left; }}
        th {{ background: #f39c12; color: white; font-weight: bold; }}
        .warning {{ background: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; }}
        .footer {{ text-align: center; margin-top: 40px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='title'>📦 HOÁS ĐƠN BÀN GIAO THIẾT BỊ</div>
        <div style='margin-top: 10px; color: #666;'>Đơn hàng #{order.Id}</div>
    </div>

    <table>
        <tr><td style='font-weight: bold;'>Khách hàng:</td><td>{order.Customer?.UserName}</td></tr>
        <tr><td style='font-weight: bold;'>Địa chỉ:</td><td>{order.Customer?.Address}</td></tr>
        <tr><td style='font-weight: bold;'>CCCD:</td><td>{order.Customer?.CccdNumber}</td></tr>
        <tr><td style='font-weight: bold;'>Ngày nhận:</td><td>{order.ExpectedPickUpTime:dd/MM/yyyy HH:mm}</td></tr>
        <tr><td style='font-weight: bold;'>Ngày trả dự kiến:</td><td>{order.ExpectedReturnTime:dd/MM/yyyy HH:mm}</td></tr>
    </table>

    <h3>Chi tiết thiết bị bàn giao:</h3>
    <table>
        <thead>
            <tr>
                <th>STT</th>
                <th>Tên thiết bị</th>
                <th>Số lượng</th>
                <th>Giá/ngày</th>
                <th>Thành tiền</th>
            </tr>
        </thead>
        <tbody>
            {detailsHtml}
        </tbody>
    </table>

    <div class='warning'>
        <strong>⚠️ NHẮC NHỠ QUAN TRỌNG:</strong><br>
        - Khách hàng vui lòng trả thiết bị đúng hạn vào ngày: <strong>{order.ExpectedReturnTime:dd/MM/yyyy HH:mm}</strong><br>
        - Nếu trả muộn sẽ được tính phí phạt: <strong>5%</strong> giá thuê/ngày<br>
        - Thiết bị phải trả trong tình trạng nguyên vẹn<br>
        - Liên hệ ngay nếu có vấn đề hoặc cần gia hạn thêm
    </div>

    <table>
        <tr><td style='font-weight: bold;'>Tổng tiền thuê ({rentalDays:F0} ngày):</td><td><strong>{order.TotalAmount:N0} VND</strong></td></tr>
        <tr><td style='font-weight: bold;'>Tiền cọc đã thu:</td><td>{order.DepositAmount:N0} VND</td></tr>
        <tr><td style='font-weight: bold;'>Còn phải thanh toán:</td><td><strong>{(order.TotalAmount - order.DepositAmount):N0} VND</strong></td></tr>
    </table>

    <div class='footer'>
        <p>Hoá đơn bàn giao - Vui lòng giữ để xác minh khi trả hàng</p>
        <p>In ngày: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>
    </div>
</body>
</html>";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int id, string messageContent)
        {
            var order = await _context.RentalOrders.FindAsync(id);
            if (order == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(messageContent))
            {
                TempData["Error"] = "Nội dung tin nhắn không được để trống!";
                return RedirectToAction("Detail", new { id });
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
            return RedirectToAction("Detail", new { id });
        }
    }
}
