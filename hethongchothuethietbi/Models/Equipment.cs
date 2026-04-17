using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hethongchothuethietbi.Models
{
    public class Equipment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên thiết bị không được để trống")]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Danh mục không được để trống")]
        public int CategoryId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Giá thuê phải lớn hơn 0")]
        public decimal PricePerDay { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng không được âm")]
        public int Quantity { get; set; } = 1;

        [Required]
        public EquipmentStatus Status { get; set; } = EquipmentStatus.Available;

        [StringLength(500)]
        public string ConditionNotes { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(CategoryId))]
        public virtual Category Category { get; set; }
    }

    public enum EquipmentStatus
    {
        Available,    // Sẵn sàng
        Rented,       // Đang thuê
        Broken,       // Hư hỏng
        Maintenance   // Bảo trì
    }
}
