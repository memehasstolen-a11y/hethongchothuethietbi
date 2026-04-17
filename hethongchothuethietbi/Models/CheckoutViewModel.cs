using System.ComponentModel.DataAnnotations;

namespace hethongchothuethietbi.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
        public DateTime ExpectedPickUpTime { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc không được để trống")]
        public DateTime ExpectedReturnTime { get; set; }

        public List<CheckoutItemViewModel> Items { get; set; } = new List<CheckoutItemViewModel>();

        public decimal TotalAmount => CalculateTotal();

        public decimal DepositAmount => TotalAmount * 0.3m;

        private decimal CalculateTotal()
        {
            decimal total = 0;
            if (Items != null && Items.Any())
            {
                var days = (decimal)Math.Ceiling((ExpectedReturnTime - ExpectedPickUpTime).TotalDays);
                foreach (var item in Items)
                {
                    total += item.PricePerDay * days * item.Quantity;
                }
            }
            return total;
        }
    }

    public class CheckoutItemViewModel
    {
        public int EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public decimal PricePerDay { get; set; }
        public int Quantity { get; set; }

        public decimal Total => PricePerDay * Quantity;
    }
}
