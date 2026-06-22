using System;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml;
using Contoso.OrderManagement.Models;

namespace Contoso.OrderManagement
{
#pragma warning disable 618
    [WebService(
        Namespace = "http://schemas.contoso.com/orders/2024/",
        Name = "OrderManagementService",
        Description = "CRUD operations for the Contoso order management system.")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class OrderService : WebService
    {
        private readonly IOrderRepository _repository;

        public AuthenticationHeader authHeader;

        public OrderService()
            : this(OrderRepository.Instance)
        {
        }

        internal OrderService(IOrderRepository repository)
        {
            _repository = repository;
        }

        [WebMethod(Description = "Gets complete Order by ID.", CacheDuration = 30)]
        public Order GetOrderById(int orderId)
        {
            var order = _repository.GetById(orderId);
            if (order == null)
            {
                throw new SoapException(
                    "Order not found.",
                    SoapException.ClientFaultCode,
                    GetRequestUrl(),
                    BuildFaultDetail("ORDER_NOT_FOUND", $"No order with ID {orderId} exists."));
            }

            return order;
        }

        [WebMethod(Description = "Returns order summaries for a given customer.")]
        public OrderSummary[] GetOrdersByCustomer(int customerId)
        {
            return _repository.GetByCustomer(customerId);
        }

        [WebMethod(Description = "Searches orders by status, date range, and/or customer name.")]
        public OrderSummary[] SearchOrders(OrderSearchCriteria criteria)
        {
            return _repository.Search(criteria);
        }

        [WebMethod(Description = "Creates a new order. Returns new OrderId.")]
        [SoapHeader("authHeader", Required = true)]
        public int CreateOrder(Order order)
        {
            ValidateToken();
            if (order == null)
            {
                throw new SoapException(
                    "Order cannot be null.",
                    SoapException.ClientFaultCode,
                    GetRequestUrl(),
                    BuildFaultDetail("INVALID_ORDER", "Order parameter is required."));
            }

            return _repository.Create(order);
        }

        [WebMethod(Description = "Updates order status.")]
        [SoapHeader("authHeader", Required = true)]
        public void UpdateOrderStatus(int orderId, OrderStatus newStatus)
        {
            ValidateToken();
            try
            {
                _repository.UpdateStatus(orderId, newStatus);
            }
            catch (ArgumentException ex)
            {
                throw new SoapException(
                    ex.Message,
                    SoapException.ClientFaultCode,
                    GetRequestUrl(),
                    BuildFaultDetail("ORDER_NOT_FOUND", ex.Message));
            }
        }

        [WebMethod(Description = "Deletes a Pending order.")]
        [SoapHeader("authHeader", Required = true)]
        public void DeleteOrder(int orderId)
        {
            ValidateToken();
            try
            {
                _repository.Delete(orderId);
            }
            catch (ArgumentException ex)
            {
                throw new SoapException(
                    ex.Message,
                    SoapException.ClientFaultCode,
                    GetRequestUrl(),
                    BuildFaultDetail("ORDER_NOT_FOUND", ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                throw new SoapException(
                    ex.Message,
                    SoapException.ClientFaultCode,
                    GetRequestUrl(),
                    BuildFaultDetail("INVALID_OPERATION", ex.Message));
            }
        }

        private void ValidateToken()
        {
            if (authHeader?.ApiToken != "DEMO-TOKEN-12345")
            {
                throw new SoapException(
                    "Unauthorized",
                    SoapException.ClientFaultCode,
                    GetRequestUrl(),
                    BuildFaultDetail("UNAUTHORIZED", "Invalid or missing API token."));
            }
        }

        private string GetRequestUrl()
        {
            try
            {
                return Context?.Request?.Url?.AbsoluteUri ?? string.Empty;
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
        }

        private static XmlNode BuildFaultDetail(string errorCode, string message)
        {
            var doc = new XmlDocument();
            var detail = doc.CreateElement("detail");
            var error = doc.CreateElement("error");
            var code = doc.CreateElement("code");
            code.InnerText = errorCode;
            var msg = doc.CreateElement("message");
            msg.InnerText = message;
            error.AppendChild(code);
            error.AppendChild(msg);
            detail.AppendChild(error);
            return detail;
        }
    }
#pragma warning restore 618
}
