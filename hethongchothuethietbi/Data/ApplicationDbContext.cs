using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using hethongchothuethietbi.Models;

namespace hethongchothuethietbi.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<RentalOrder> RentalOrders { get; set; }
        public DbSet<RentalOrderDetail> RentalOrderDetails { get; set; }
        public DbSet<OrderComment> OrderComments { get; set; }
        public DbSet<MaintenanceLog> MaintenanceLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Cấu hình Decimal Precision
            builder.Entity<Equipment>()
                .Property(e => e.PricePerDay)
                .HasPrecision(10, 2);

            builder.Entity<RentalOrder>()
                .Property(ro => ro.TotalAmount)
                .HasPrecision(10, 2);

            builder.Entity<RentalOrder>()
                .Property(ro => ro.DepositAmount)
                .HasPrecision(10, 2);

            builder.Entity<RentalOrder>()
                .Property(ro => ro.PenaltyAmount)
                .HasPrecision(10, 2);

            builder.Entity<RentalOrderDetail>()
                .Property(rod => rod.UnitPriceAtBooking)
                .HasPrecision(10, 2);

            // Cấu hình quan hệ Category -> Equipment (1-N)
            builder.Entity<Equipment>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Equipments)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ RentalOrder -> RentalOrderDetail (1-N)
            builder.Entity<RentalOrderDetail>()
                .HasOne(rod => rod.RentalOrder)
                .WithMany(ro => ro.Details)
                .HasForeignKey(rod => rod.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ Equipment -> RentalOrderDetail (1-N)
            builder.Entity<RentalOrderDetail>()
                .HasOne(rod => rod.Equipment)
                .WithMany()
                .HasForeignKey(rod => rod.EquipmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ RentalOrder -> OrderComment (1-N)
            builder.Entity<OrderComment>()
                .HasOne(oc => oc.RentalOrder)
                .WithMany(ro => ro.Comments)
                .HasForeignKey(oc => oc.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Cấu hình quan hệ AppUser -> RentalOrder (1-N)
            builder.Entity<RentalOrder>()
                .HasOne(ro => ro.Customer)
                .WithMany(u => u.RentalOrders)
                .HasForeignKey(ro => ro.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình quan hệ Equipment -> MaintenanceLog (1-N)
            builder.Entity<MaintenanceLog>()
                .HasOne(ml => ml.Equipment)
                .WithMany()
                .HasForeignKey(ml => ml.EquipmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
