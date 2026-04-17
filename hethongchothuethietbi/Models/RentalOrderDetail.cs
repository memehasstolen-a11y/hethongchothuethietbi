using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hethongchothuethietbi.Models
{
    public class RentalOrderDetail
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal UnitPriceAtBooking { get; set; } // Price Persistence

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        // Navigation Properties
        [ForeignKey(nameof(OrderId))]
        public virtual RentalOrder RentalOrder { get; set; }

        [ForeignKey(nameof(EquipmentId))]
        public virtual Equipment Equipment { get; set; }
    }
}
