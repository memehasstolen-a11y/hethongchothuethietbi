using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hethongchothuethietbi.Models
{
    public class RentalOrder
    {
        public int Id { get; set; }

        [Required]
        public string CustomerId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DepositAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PenaltyAmount { get; set; } = 0;

        [Required]
        public RentalOrderStatus OrderStatus { get; set; } = RentalOrderStatus.PendingPayment;

        [Required]
        public DateTime ExpectedPickUpTime { get; set; }

        [Required]
        public DateTime ExpectedReturnTime { get; set; }

        public DateTime? ActualReturnTime { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false; // Soft delete for customers

        // Navigation Properties
        [ForeignKey(nameof(CustomerId))]
        public virtual AppUser Customer { get; set; }
        public virtual ICollection<RentalOrderDetail> Details { get; set; } = new List<RentalOrderDetail>();
        public virtual ICollection<OrderComment> Comments { get; set; } = new List<OrderComment>();
    }

    public enum RentalOrderStatus
    {
        PendingPayment,  // Chờ thanh toán cọc
        Deposited,       // Đã cọc
        Rented,          // Đang thuê
        Completed,       // Hoàn thành
        Cancelled,       // Hủy
        Lost             // Mất thiết bị
    }
}
