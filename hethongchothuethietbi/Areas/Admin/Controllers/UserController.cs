using hethongchothuethietbi.Data;
using hethongchothuethietbi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace hethongchothuethietbi.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Admin/User
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // POST: Create Staff/Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppUser model, string role, string Password)
        {
            if (!string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(Password))
            {
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Address = model.Address ?? "",
                    CccdNumber = model.CccdNumber ?? ""
                };

                // Sử dụng mật khẩu từ form
                var result = await _userManager.CreateAsync(user, Password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(role))
                    {
                        await _userManager.AddToRoleAsync(user, role);
                    }

                    TempData["Success"] = $"Tạo tài khoản {role} thành công!";
                    return RedirectToAction("Index");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(model.Email))
                    ModelState.AddModelError(string.Empty, "Email là bắt buộc!");
                if (string.IsNullOrEmpty(Password))
                    ModelState.AddModelError(string.Empty, "Mật khẩu là bắt buộc!");
            }

            return View("Index", await _context.Users.ToListAsync());
        }

        // POST: Assign Role
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !string.IsNullOrEmpty(role))
            {
                var roles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, roles);
                await _userManager.AddToRoleAsync(user, role);

                TempData["Success"] = "Cấp quyền thành công!";
            }

            return RedirectToAction("Index");
        }

        // POST: Delete User
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["Error"] = "Người dùng không tồn tại!";
                return RedirectToAction("Index");
            }

            // Delete all user's orders first (cascade)
            var userOrders = await _context.RentalOrders
                .Where(o => o.CustomerId == userId)
                .ToListAsync();

            foreach (var order in userOrders)
            {
                // Delete order details first
                var details = await _context.RentalOrderDetails
                    .Where(d => d.OrderId == order.Id)
                    .ToListAsync();
                _context.RentalOrderDetails.RemoveRange(details);

                // Delete order comments
                var comments = await _context.OrderComments
                    .Where(c => c.OrderId == order.Id)
                    .ToListAsync();
                _context.OrderComments.RemoveRange(comments);

                // Delete order
                _context.RentalOrders.Remove(order);
            }

            await _context.SaveChangesAsync();

            // Now delete user
            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Xóa tài khoản thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể xóa tài khoản. Vui lòng thử lại.";
            }

            return RedirectToAction("Index");
        }
    }
}
