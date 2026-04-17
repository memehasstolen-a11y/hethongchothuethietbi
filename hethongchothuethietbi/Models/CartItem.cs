namespace hethongchothuethietbi.Models
{
    public class CartItem
    {
        public int EquipmentId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class CartViewModel
    {
        public int EquipmentId { get; set; }
        public string EquipmentName { get; set; }
        public decimal PricePerDay { get; set; }
        public int Quantity { get; set; }
        public decimal Total { get; set; }
    }
}
