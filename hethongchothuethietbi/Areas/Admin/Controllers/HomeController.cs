using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hethongchothuethietbi.Data;
using Microsoft.EntityFrameworkCore;

namespace hethongchothuethietbi.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dashboard
        public async Task<IActionResult> Index()
        {
            var totalOrders = await _context.RentalOrders.CountAsync();
            var totalRevenue = await _context.RentalOrders.SumAsync(o => o.TotalAmount);
            var totalEquipment = await _context.Equipments.CountAsync();
            var brokenEquipment = await _context.Equipments.Where(e => e.Status == Models.EquipmentStatus.Broken).CountAsync();
            var pendingOrders = await _context.RentalOrders.Where(o => o.OrderStatus == Models.RentalOrderStatus.PendingPayment).CountAsync();

            ViewBag.TotalOrders = totalOrders;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalEquipment = totalEquipment;
            ViewBag.BrokenEquipment = brokenEquipment;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.Title = "Dashboard";

            return View();
        }
    }
}
