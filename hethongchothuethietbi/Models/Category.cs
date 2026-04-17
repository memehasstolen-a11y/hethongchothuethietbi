using System.ComponentModel.DataAnnotations;

namespace hethongchothuethietbi.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // Navigation Properties
        public virtual ICollection<Equipment> Equipments { get; set; } = new List<Equipment>();
    }
}
