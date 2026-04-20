using hethongchothuethietbi.Data;
using hethongchothuethietbi.Extensions;
using hethongchothuethietbi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hethongchothuethietbi.Controllers
{
    public class EquipmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EquipmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Equipment/Index
        public async Task<IActionResult> Index(int page = 1, string search = "", string sort = "name")
        {
            int pageSize = 9;
            IQueryable<Equipment> query = _context.Equipments
                .Include(e => e.Category);

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.Name.Contains(search) || e.Category.Name.Contains(search));
            }

            // Sort
            query = sort switch
            {
                "price_asc" => query.OrderBy(e => e.PricePerDay),
                "price_desc" => query.OrderByDescending(e => e.PricePerDay),
                _ => query.OrderBy(e => e.Name)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.Sort = sort;

            return View(items);
        }

        // GET: Equipment/Details
        public async Task<IActionResult> Details(int id)
        {
            var equipment = await _context.Equipments
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (equipment == null)
                return NotFound();

            return View(equipment);
        }

        // POST: Add to Cart
        [HttpPost]
        public IActionResult AddToCart(int equipmentId, int quantity = 1)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.EquipmentId == equipmentId);

            if (item != null)
                item.Quantity += quantity;
            else
                cart.Add(new CartItem { EquipmentId = equipmentId, Quantity = quantity });

            HttpContext.Session.SetObject("Cart", cart);
            return Json(new { success = true, message = "Thêm vào giỏ thành công!" });
        }
    }

    public class CartItem
    {
        public int EquipmentId { get; set; }
        public int Quantity { get; set; }
    }
}
