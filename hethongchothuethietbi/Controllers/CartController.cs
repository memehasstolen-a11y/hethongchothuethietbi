using hethongchothuethietbi.Data;
using hethongchothuethietbi.Extensions;
using hethongchothuethietbi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hethongchothuethietbi.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: Add to Cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int equipmentId)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            var existingItem = cart.FirstOrDefault(c => c.EquipmentId == equipmentId);

            if (existingItem != null)
            {
                existingItem.Quantity += 1;
            }
            else
            {
                cart.Add(new CartItem { EquipmentId = equipmentId, Quantity = 1 });
            }

            HttpContext.Session.SetObject("Cart", cart);

            var cartCount = cart.Sum(c => c.Quantity);
            TempData["Success"] = "Đã thêm vào giỏ hàng!";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, cartCount = cartCount, message = "Đã thêm vào giỏ hàng!" });
            }

            return RedirectToAction("Index");
        }

        // GET: Cart/Index
        public async Task<IActionResult> Index()
        {
            var cartItems = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            var equipmentIds = cartItems.Select(c => c.EquipmentId).ToList();

            var equipments = await _context.Equipments
                .Where(e => equipmentIds.Contains(e.Id))
                .ToListAsync();

            var cartView = new List<Models.CartViewModel>();
            foreach (var item in cartItems)
            {
                var equipment = equipments.FirstOrDefault(e => e.Id == item.EquipmentId);
                if (equipment != null)
                {
                    cartView.Add(new Models.CartViewModel
                    {
                        EquipmentId = equipment.Id,
                        EquipmentName = equipment.Name,
                        PricePerDay = equipment.PricePerDay,
                        Quantity = item.Quantity,
                        Total = equipment.PricePerDay * item.Quantity
                    });
                }
            }

            return View(cartView);
        }

        // POST: Remove from Cart
        [HttpPost]
        public IActionResult Remove(int equipmentId)
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            cart.RemoveAll(c => c.EquipmentId == equipmentId);
            HttpContext.Session.SetObject("Cart", cart);

            return RedirectToAction("Index");
        }

        // POST: Clear Cart
        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove("Cart");
            return RedirectToAction("Index");
        }

        // POST: Update Quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int equipmentId, int quantity)
        {
            var equipment = await _context.Equipments.FindAsync(equipmentId);

            // Validate quantity is positive and not exceeding available stock
            if (quantity <= 0 || equipment == null || quantity > equipment.Quantity)
            {
                TempData["Error"] = "Số lượng không hợp lệ hoặc vượt quá tồn kho!";
                return RedirectToAction("Index");
            }

            var cart = HttpContext.Session.GetObject<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.EquipmentId == equipmentId);

            if (item != null)
            {
                item.Quantity = quantity;
                HttpContext.Session.SetObject("Cart", cart);
                TempData["Success"] = "Cập nhật số lượng thành công!";
            }

            return RedirectToAction("Index");
        }
    }
}

