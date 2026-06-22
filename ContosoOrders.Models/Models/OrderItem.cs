namespace Contoso.OrderManagement.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public string ProductName { get; set; }
        public string Sku { get; set; }
        public ProductCategory Category { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
