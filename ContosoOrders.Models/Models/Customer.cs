namespace Contoso.OrderManagement.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public Address ShippingAddress { get; set; }
    }
}
