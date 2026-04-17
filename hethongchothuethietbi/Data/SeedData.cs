using hethongchothuethietbi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace hethongchothuethietbi.Data
{
    public class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            // Đảm bảo Database được tạo
            await context.Database.MigrateAsync();

            // Tạo 3 Roles
            var roles = new[] { "Admin", "Staff", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Tạo tài khoản Admin mặc định
            var adminEmail = "admin@gmail.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrator",
                    Address = "Ha Noi",
                    CccdNumber = "000000000000",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "123456!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Seed Categories
            if (!await context.Categories.AnyAsync())
            {
                var categories = new[]
                {
                    new Category { Name = "Loa", Description = "Các loại loa âm thanh chuyên nghiệp" },
                    new Category { Name = "Micro", Description = "Microphone cho sự kiện, karaoke, livestream" },
                    new Category { Name = "Máy Ảnh", Description = "Máy ảnh DSLR, mirrorless chuyên nghiệp" },
                    new Category { Name = "Đèn Sân Khấu", Description = "Đèn chiếu sáng cho sự kiện" },
                    new Category { Name = "Bộ Âm Thanh", Description = "Bộ dàn âm thanh hoàn chỉnh" }
                };

                foreach (var category in categories)
                {
                    context.Categories.Add(category);
                }
                await context.SaveChangesAsync();
            }

            // Seed Equipment
            if (!await context.Equipments.AnyAsync())
            {
                var categories = await context.Categories.ToListAsync();
                var equipments = new[]
                {
                    // Loa
                    new Equipment
                    {
                        Name = "Loa JBL PRX815XLFW",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Loa")?.Id ?? 0,
                        PricePerDay = 500000,
                        Quantity = 2,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Loa 15 inch công suất 1500W, tình trạng rất tốt"
                    },
                    new Equipment
                    {
                        Name = "Loa Bose S1 Pro",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Loa")?.Id ?? 0,
                        PricePerDay = 350000,
                        Quantity = 3,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Loa di động, âm thanh sắc nét"
                    },
                    new Equipment
                    {
                        Name = "Loa Yamaha DBR15",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Loa")?.Id ?? 0,
                        PricePerDay = 400000,
                        Quantity = 2,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Loa PA 15 inch, âm bass mạnh"
                    },

                    // Micro
                    new Equipment
                    {
                        Name = "Microphone Shure SM7B",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Micro")?.Id ?? 0,
                        PricePerDay = 150000,
                        Quantity = 5,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Mic chuyên nghiệp, âm thanh studio chất lượng"
                    },
                    new Equipment
                    {
                        Name = "Microphone Shure BETA 58A",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Micro")?.Id ?? 0,
                        PricePerDay = 100000,
                        Quantity = 10,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Mic karaoke, phù hợp sân khấu"
                    },
                    new Equipment
                    {
                        Name = "Microphone Rode Wireless GO",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Micro")?.Id ?? 0,
                        PricePerDay = 120000,
                        Quantity = 4,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Mic không dây cho livestream, video"
                    },
                    new Equipment
                    {
                        Name = "Microphone Condenser NEUMANN U87",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Micro")?.Id ?? 0,
                        PricePerDay = 200000,
                        Quantity = 2,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Mic condenser cao cấp cho studio"
                    },

                    // Máy Ảnh
                    new Equipment
                    {
                        Name = "Canon EOS R5",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Máy Ảnh")?.Id ?? 0,
                        PricePerDay = 800000,
                        Quantity = 1,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Mirrorless 45MP, quay video 8K"
                    },
                    new Equipment
                    {
                        Name = "Canon EOS 5D Mark IV",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Máy Ảnh")?.Id ?? 0,
                        PricePerDay = 700000,
                        Quantity = 2,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "DSLR full-frame 30MP, đã sử dụng 2 năm"
                    },
                    new Equipment
                    {
                        Name = "Sony A7IV",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Máy Ảnh")?.Id ?? 0,
                        PricePerDay = 750000,
                        Quantity = 1,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Mirrorless 61MP, hiệu suất cao"
                    },
                    new Equipment
                    {
                        Name = "Nikon Z9",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Máy Ảnh")?.Id ?? 0,
                        PricePerDay = 850000,
                        Quantity = 1,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Mirrorless chuyên nghiệp chụp thể thao"
                    },

                    // Đèn Sân Khấu
                    new Equipment
                    {
                        Name = "Đèn Par 64 LED RGB",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Đèn Sân Khấu")?.Id ?? 0,
                        PricePerDay = 200000,
                        Quantity = 6,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Đèn LED RGB điều khiển DMX"
                    },
                    new Equipment
                    {
                        Name = "Đèn Moving Head Beam 200W",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Đèn Sân Khấu")?.Id ?? 0,
                        PricePerDay = 400000,
                        Quantity = 3,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Đèn chiếu sáng chuyên nghiệp, xoay 360 độ"
                    },

                    // Bộ Âm Thanh
                    new Equipment
                    {
                        Name = "Bộ âm thanh Karaoke cao cấp",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Bộ Âm Thanh")?.Id ?? 0,
                        PricePerDay = 1200000,
                        Quantity = 2,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Bộ dàn âm thanh hoàn chỉnh gồm: 2 loa, 2 micro, mixer"
                    },
                    new Equipment
                    {
                        Name = "Bộ âm thanh sự kiện",
                        CategoryId = categories.FirstOrDefault(c => c.Name == "Bộ Âm Thanh")?.Id ?? 0,
                        PricePerDay = 1500000,
                        Quantity = 1,
                        Status = EquipmentStatus.Available,
                        ConditionNotes = "Âm thanh cho sự kiện ngoài trời, công suất 5000W"
                    }
                };

                foreach (var equipment in equipments)
                {
                    context.Equipments.Add(equipment);
                }
                await context.SaveChangesAsync();
            }
        }
    }
}
