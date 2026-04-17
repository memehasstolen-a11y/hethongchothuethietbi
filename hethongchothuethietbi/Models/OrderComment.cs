using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hethongchothuethietbi.Models
{
    public class OrderComment
    {
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public string AuthorId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; }

        [StringLength(500)]
        public string ImageUrl { get; set; } // UNC path hoặc URL hình ảnh biên lai

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey(nameof(OrderId))]
        public virtual RentalOrder RentalOrder { get; set; }

        [ForeignKey(nameof(AuthorId))]
        public virtual AppUser Author { get; set; }
    }
}
