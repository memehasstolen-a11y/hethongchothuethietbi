using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using hethongchothuethietbi.Data;
using hethongchothuethietbi.Models;
using Microsoft.EntityFrameworkCore;

namespace hethongchothuethietbi.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EquipmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EquipmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Equipment
        public async Task<IActionResult> Index(int page = 1, string search = "", string sort = "name")
        {
            int pageSize = 10;
            IQueryable<Equipment> query = _context.Equipments.Include(e => e.Category);

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
                "status" => query.OrderBy(e => e.Status),
                _ => query.OrderBy(e => e.Name)
            };

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Total = total;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;
            ViewBag.Sort = sort;
            ViewBag.Title = "Quản Lý Thiết Bị";

            return View(items);
        }

        // GET: Admin/Equipment/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        // POST: Admin/Equipment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Equipment equipment)
        {
            // Validate CategoryId >= 1
            if (equipment.CategoryId <= 0)
            {
                ModelState.AddModelError(nameof(Equipment.CategoryId), "Vui lòng chọn danh mục");
            }

            // Validate Name
            if (string.IsNullOrWhiteSpace(equipment.Name))
            {
                ModelState.AddModelError(nameof(Equipment.Name), "Tên thiết bị không được để trống");
            }

            // Validate PricePerDay
            if (equipment.PricePerDay <= 0)
            {
                ModelState.AddModelError(nameof(Equipment.PricePerDay), "Giá thuê phải lớn hơn 0");
            }

            // Validate Quantity
            if (equipment.Quantity < 1)
            {
                ModelState.AddModelError(nameof(Equipment.Quantity), "Số lượng phải >= 1");
            }

            if (ModelState.IsValid)
            {
                equipment.Status = EquipmentStatus.Available;
                _context.Equipments.Add(equipment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tạo thiết bị thành công!";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(equipment);
        }

        // GET: Admin/Equipment/Edit
        public async Task<IActionResult> Edit(int id)
        {
            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment == null)
                return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(equipment);
        }

        // POST: Admin/Equipment/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Equipment equipment)
        {
            if (id != equipment.Id)
                return NotFound();

            // Validate CategoryId >= 1
            if (equipment.CategoryId <= 0)
            {
                ModelState.AddModelError(nameof(Equipment.CategoryId), "Vui lòng chọn danh mục");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(equipment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật thiết bị thành công!";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    return NotFound();
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(equipment);
        }

        // POST: Admin/Equipment/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment == null)
            {
                TempData["Error"] = "Thiết bị không tồn tại!";
                return RedirectToAction("Index");
            }

            _context.Equipments.Remove(equipment);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Xóa thiết bị thành công!";
            return RedirectToAction("Index");
        }
    }
}
