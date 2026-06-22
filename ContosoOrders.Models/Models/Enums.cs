using System.Xml.Serialization;

namespace Contoso.OrderManagement.Models
{
    public enum OrderStatus
    {
        [XmlEnum("Pending")] Pending,
        [XmlEnum("Processing")] Processing,
        [XmlEnum("Shipped")] Shipped,
        [XmlEnum("Delivered")] Delivered,
        [XmlEnum("Cancelled")] Cancelled
    }

    public enum ProductCategory
    {
        [XmlEnum("Electronics")] Electronics,
        [XmlEnum("Office")] Office,
        [XmlEnum("Industrial")] Industrial,
        [XmlEnum("Other")] Other
    }
}
