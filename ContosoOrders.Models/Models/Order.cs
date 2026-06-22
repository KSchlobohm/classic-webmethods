using System;
using System.Xml.Serialization;

namespace Contoso.OrderManagement.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime ShippedDate { get; set; }
        public bool ShippedDateSpecified { get; set; }
        public OrderStatus Status { get; set; }
        public Customer Customer { get; set; }

        [XmlArray("OrderItems")]
        [XmlArrayItem("Item")]
        public OrderItem[] Items { get; set; }

        [XmlElement(IsNullable = true)]
        public string Notes { get; set; }
    }
}
