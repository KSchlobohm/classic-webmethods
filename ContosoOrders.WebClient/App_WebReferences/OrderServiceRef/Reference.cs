using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Web.Services;
using System.Web.Services.Description;
using System.Web.Services.Protocols;
using System.Xml.Serialization;

namespace Contoso.OrderClient
{
    [DebuggerStepThrough]
    [DesignerCategory("code")]
    [WebServiceBinding(Name = "OrderManagementServiceSoap", Namespace = "http://schemas.contoso.com/orders/2024/")]
    public class OrderManagementService : SoapHttpClientProtocol
    {
        public AuthenticationHeader AuthenticationHeaderValue;

        public OrderManagementService()
        {
            Url = "http://localhost:54321/OrderService.asmx";
        }

        [SoapDocumentMethod(
            "http://schemas.contoso.com/orders/2024/GetOrderById",
            RequestNamespace = "http://schemas.contoso.com/orders/2024/",
            ResponseNamespace = "http://schemas.contoso.com/orders/2024/",
            Use = SoapBindingUse.Literal,
            ParameterStyle = SoapParameterStyle.Wrapped)]
        public Order GetOrderById(int orderId)
        {
            var results = Invoke("GetOrderById", new object[] { orderId });
            return (Order)results[0];
        }

        [SoapDocumentMethod(
            "http://schemas.contoso.com/orders/2024/GetOrdersByCustomer",
            RequestNamespace = "http://schemas.contoso.com/orders/2024/",
            ResponseNamespace = "http://schemas.contoso.com/orders/2024/",
            Use = SoapBindingUse.Literal,
            ParameterStyle = SoapParameterStyle.Wrapped)]
        public OrderSummary[] GetOrdersByCustomer(int customerId)
        {
            var results = Invoke("GetOrdersByCustomer", new object[] { customerId });
            return (OrderSummary[])results[0];
        }

        [SoapDocumentMethod(
            "http://schemas.contoso.com/orders/2024/SearchOrders",
            RequestNamespace = "http://schemas.contoso.com/orders/2024/",
            ResponseNamespace = "http://schemas.contoso.com/orders/2024/",
            Use = SoapBindingUse.Literal,
            ParameterStyle = SoapParameterStyle.Wrapped)]
        public OrderSummary[] SearchOrders(OrderSearchCriteria criteria)
        {
            var results = Invoke("SearchOrders", new object[] { criteria });
            return (OrderSummary[])results[0];
        }

        [SoapHeader("AuthenticationHeaderValue")]
        [SoapDocumentMethod(
            "http://schemas.contoso.com/orders/2024/CreateOrder",
            RequestNamespace = "http://schemas.contoso.com/orders/2024/",
            ResponseNamespace = "http://schemas.contoso.com/orders/2024/",
            Use = SoapBindingUse.Literal,
            ParameterStyle = SoapParameterStyle.Wrapped)]
        public int CreateOrder(Order order)
        {
            var results = Invoke("CreateOrder", new object[] { order });
            return (int)results[0];
        }

        [SoapHeader("AuthenticationHeaderValue")]
        [SoapDocumentMethod(
            "http://schemas.contoso.com/orders/2024/UpdateOrderStatus",
            RequestNamespace = "http://schemas.contoso.com/orders/2024/",
            ResponseNamespace = "http://schemas.contoso.com/orders/2024/",
            Use = SoapBindingUse.Literal,
            ParameterStyle = SoapParameterStyle.Wrapped)]
        public void UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            Invoke("UpdateOrderStatus", new object[] { orderId, newStatus });
        }

        [SoapHeader("AuthenticationHeaderValue")]
        [SoapDocumentMethod(
            "http://schemas.contoso.com/orders/2024/DeleteOrder",
            RequestNamespace = "http://schemas.contoso.com/orders/2024/",
            ResponseNamespace = "http://schemas.contoso.com/orders/2024/",
            Use = SoapBindingUse.Literal,
            ParameterStyle = SoapParameterStyle.Wrapped)]
        public void DeleteOrder(int orderId)
        {
            Invoke("DeleteOrder", new object[] { orderId });
        }
    }

    [Serializable]
    [XmlType(Namespace = "http://schemas.contoso.com/orders/2024/")]
    public class Order
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime ShippedDate { get; set; }
        [XmlIgnore]
        public bool ShippedDateSpecified { get; set; }
        public OrderStatus Status { get; set; }
        public Customer Customer { get; set; }

        [XmlArray("OrderItems")]
        [XmlArrayItem("Item")]
        public OrderItem[] Items { get; set; }

        [XmlElement(IsNullable = true)]
        public string Notes { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = "http://schemas.contoso.com/orders/2024/")]
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public string ProductName { get; set; }
        public string Sku { get; set; }
        public ProductCategory Category { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = "http://schemas.contoso.com/orders/2024/")]
    public class Customer
    {
        public int CustomerId { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public Address ShippingAddress { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = "http://schemas.contoso.com/orders/2024/")]
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string StateProvince { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = "http://schemas.contoso.com/orders/2024/")]
    public class OrderSummary
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public string CustomerName { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = "http://schemas.contoso.com/orders/2024/")]
    public class OrderSearchCriteria
    {
        [XmlElement(IsNullable = true)]
        public string CustomerName { get; set; }

        public OrderStatus Status { get; set; }
        [XmlIgnore]
        public bool StatusSpecified { get; set; }
        public DateTime FromDate { get; set; }
        [XmlIgnore]
        public bool FromDateSpecified { get; set; }
        public DateTime ToDate { get; set; }
        [XmlIgnore]
        public bool ToDateSpecified { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = "http://schemas.contoso.com/orders/2024/")]
    public class AuthenticationHeader : SoapHeader
    {
        public string ApiToken { get; set; }
        public string ClientId { get; set; }
    }

    [Serializable]
    [XmlType(Namespace = "http://schemas.contoso.com/orders/2024/")]
    public enum OrderStatus
    {
        [XmlEnum("Pending")]
        Pending,
        [XmlEnum("Processing")]
        Processing,
        [XmlEnum("Shipped")]
        Shipped,
        [XmlEnum("Delivered")]
        Delivered,
        [XmlEnum("Cancelled")]
        Cancelled
    }

    [Serializable]
    [XmlType(Namespace = "http://schemas.contoso.com/orders/2024/")]
    public enum ProductCategory
    {
        [XmlEnum("Electronics")]
        Electronics,
        [XmlEnum("Office")]
        Office,
        [XmlEnum("Industrial")]
        Industrial,
        [XmlEnum("Other")]
        Other
    }
}
