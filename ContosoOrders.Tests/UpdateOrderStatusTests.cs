using System.Web.Services.Protocols;
using Contoso.OrderManagement;
using Contoso.OrderManagement.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Contoso.OrderManagement.Tests
{
    [TestClass]
    public class UpdateOrderStatusTests
    {
        private static OrderService CreateService(IOrderRepository repo)
        {
            var svc = new OrderService(repo);
            svc.authHeader = new AuthenticationHeader { ApiToken = "DEMO-TOKEN-12345", ClientId = "test" };
            return svc;
        }

        [TestMethod]
        public void UpdateOrderStatus_Succeeds()
        {
            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.UpdateStatus(1, OrderStatus.Shipped));
            var svc = CreateService(repo.Object);
            svc.UpdateOrderStatus(1, OrderStatus.Shipped);
            repo.Verify(r => r.UpdateStatus(1, OrderStatus.Shipped), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(SoapException))]
        public void UpdateOrderStatus_ThrowsSoapException_WhenTokenInvalid()
        {
            var repo = new Mock<IOrderRepository>();
            var svc = new OrderService(repo.Object);
            svc.authHeader = new AuthenticationHeader { ApiToken = "BAD-TOKEN" };
            svc.UpdateOrderStatus(1, OrderStatus.Shipped);
        }

        [TestMethod]
        [ExpectedException(typeof(SoapException))]
        public void UpdateOrderStatus_ThrowsSoapException_WhenOrderNotFound()
        {
            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.UpdateStatus(99, It.IsAny<OrderStatus>()))
                .Throws(new System.ArgumentException("Order 99 not found."));
            var svc = CreateService(repo.Object);
            svc.UpdateOrderStatus(99, OrderStatus.Shipped);
        }
    }
}
