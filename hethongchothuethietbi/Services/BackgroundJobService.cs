using hethongchothuethietbi.Data;
using hethongchothuethietbi.Models;
using Microsoft.EntityFrameworkCore;

namespace hethongchothuethietbi.Services
{
    public class BackgroundJobService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BackgroundJobService> _logger;

        public BackgroundJobService(ApplicationDbContext context, ILogger<BackgroundJobService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Job 1: Quét đơn "Chờ cọc" (PendingPayment) quá 24h -> Hủy + Cộng lại tồn kho
        public async Task CancelExpiredPendingOrders()
        {
            try
            {
                var expiredOrders = await _context.RentalOrders
                    .Where(o => o.OrderStatus == RentalOrderStatus.PendingPayment &&
                                o.CreatedDate.AddHours(24) < DateTime.UtcNow)
                    .Include(o => o.Details)
                    .ToListAsync();

                foreach (var order in expiredOrders)
                {
                    // Cộng lại tồn kho
                    foreach (var detail in order.Details)
                    {
                        var equipment = await _context.Equipments.FindAsync(detail.EquipmentId);
                        if (equipment != null)
                        {
                            equipment.Quantity += 1;
                        }
                    }

                    // Đổi trạng thái
                    order.OrderStatus = RentalOrderStatus.Cancelled;
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Cancelled {expiredOrders.Count} expired pending orders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CancelExpiredPendingOrders");
            }
        }

        // Job 2: Quét đơn "Đang thuê" (Rented) trước 12h hết hạn -> Báo thông báo
        public async Task NotifyUpcomingReturnDates()
        {
            try
            {
                var upcomingOrders = await _context.RentalOrders
                    .Where(o => o.OrderStatus == RentalOrderStatus.Rented &&
                                o.ExpectedReturnTime > DateTime.UtcNow &&
                                o.ExpectedReturnTime <= DateTime.UtcNow.AddHours(12))
                    .Include(o => o.Customer)
                    .ToListAsync();

                foreach (var order in upcomingOrders)
                {
                    // Tạo OrderComment với loại thông báo (notification)
                    var notification = new OrderComment
                    {
                        OrderId = order.Id,
                        AuthorId = "SYSTEM", // System notification
                        Content = $"Nhắc nhở: Bạn cần trả thiết bị trước {order.ExpectedReturnTime:dd/MM/yyyy HH:mm}",
                        CreatedDate = DateTime.UtcNow,
                        ImageUrl = null
                    };
                    _context.OrderComments.Add(notification);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Created {upcomingOrders.Count} return date notifications");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NotifyUpcomingReturnDates");
            }
        }

        // Job 3: Quét đơn "Đang thuê" quá hạn -> Tự động +5% phí phạt
        public async Task ApplyLatePenalty()
        {
            try
            {
                var lateOrders = await _context.RentalOrders
                    .Where(o => o.OrderStatus == RentalOrderStatus.Rented &&
                                o.ExpectedReturnTime < DateTime.UtcNow)
                    .ToListAsync();

                foreach (var order in lateOrders)
                {
                    if (order.PenaltyAmount == 0) // Tránh tính lại nếu đã có phạt
                    {
                        order.PenaltyAmount = order.TotalAmount * 0.05m;
                        order.TotalAmount += order.PenaltyAmount;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Applied late penalties to {lateOrders.Count} orders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ApplyLatePenalty");
            }
        }

        // Job 4: Quét đơn quá hạn 5 ngày -> "Mất thiết bị" (Lost) -> Gửi thông báo quản lý
        public async Task MarkLostEquipment()
        {
            try
            {
                var lostOrders = await _context.RentalOrders
                    .Where(o => o.OrderStatus == RentalOrderStatus.Rented &&
                                o.ExpectedReturnTime.AddDays(5) < DateTime.UtcNow)
                    .Include(o => o.Details)
                    .Include(o => o.Customer)
                    .ToListAsync();

                foreach (var order in lostOrders)
                {
                    order.OrderStatus = RentalOrderStatus.Lost;

                    // Tạo notification cho Admin
                    var adminNotification = new OrderComment
                    {
                        OrderId = order.Id,
                        AuthorId = "SYSTEM",
                        Content = $"⚠️ CẢNH BÁO: Đơn ID {order.Id} của khách {order.Customer?.UserName} " +
                                  $"đã quá hạn 5 ngày. Tình trạng: Mất thiết bị",
                        CreatedDate = DateTime.UtcNow,
                        ImageUrl = null
                    };
                    _context.OrderComments.Add(adminNotification);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Marked {lostOrders.Count} orders as Lost");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MarkLostEquipment");
            }
        }
    }
}
