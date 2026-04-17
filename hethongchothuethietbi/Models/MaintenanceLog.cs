using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace hethongchothuethietbi.Models
{
    public class MaintenanceLog
    {
        public int Id { get; set; }

        [Required]
        public int EquipmentId { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(EquipmentId))]
        public virtual Equipment Equipment { get; set; }
    }
}
