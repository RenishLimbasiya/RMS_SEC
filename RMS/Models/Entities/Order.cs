namespace RMS.Models.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int TableId { get; set; }
        public RestaurantTable? Table { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending"; // Pending, InKitchen, Ready, Billed

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        public decimal Discount { get; set; } 
        public decimal TaxPercent { get; set; } = 5;
        public bool ReadyForBilling { get; set; } = false;

        
        public Bill? Bill { get; set; }
    }
}
