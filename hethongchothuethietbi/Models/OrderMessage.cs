using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hethongchothuethietbi.Models
{
    public class OrderMessage
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public string SenderId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        [ForeignKey(nameof(OrderId))]
        public virtual RentalOrder Order { get; set; }

        [ForeignKey(nameof(SenderId))]
        public virtual AppUser Sender { get; set; }
    }
}
