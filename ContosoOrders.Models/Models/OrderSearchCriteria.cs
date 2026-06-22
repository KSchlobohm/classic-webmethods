using System;
using System.Xml.Serialization;

namespace Contoso.OrderManagement.Models
{
    public class OrderSearchCriteria
    {
        [XmlElement(IsNullable = true)]
        public string CustomerName { get; set; }

        public OrderStatus Status { get; set; }
        public bool StatusSpecified { get; set; }
        public DateTime FromDate { get; set; }
        public bool FromDateSpecified { get; set; }
        public DateTime ToDate { get; set; }
        public bool ToDateSpecified { get; set; }
    }
}
