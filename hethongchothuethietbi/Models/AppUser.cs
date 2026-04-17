using Microsoft.AspNetCore.Identity;

namespace hethongchothuethietbi.Models
{
    public class AppUser : IdentityUser
    {
        public string FullName { get; set; }
        public string Address { get; set; }
        public string CccdNumber { get; set; }

        // Navigation Properties
        public virtual ICollection<RentalOrder> RentalOrders { get; set; } = new List<RentalOrder>();
    }
}
