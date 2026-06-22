using System.Web.Services.Protocols;
using Contoso.OrderManagement;
using Contoso.OrderManagement.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Contoso.OrderManagement.Tests
{
    [TestClass]
    public class CreateOrderTests
    {
        private static OrderService CreateService(IOrderRepository repo)
        {
            var svc = new OrderService(repo);
            svc.authHeader = new AuthenticationHeader { ApiToken = "DEMO-TOKEN-12345", ClientId = "test" };
            return svc;
        }

        [TestMethod]
        public void CreateOrder_ReturnsNewId()
        {
            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.Create(It.IsAny<Order>())).Returns(9);
            var svc = CreateService(repo.Object);
            var result = svc.CreateOrder(new Order { Status = OrderStatus.Pending });
            Assert.AreEqual(9, result);
        }

        [TestMethod]
        [ExpectedException(typeof(SoapException))]
        public void CreateOrder_ThrowsSoapException_WhenTokenInvalid()
        {
            var repo = new Mock<IOrderRepository>();
            var svc = new OrderService(repo.Object);
            svc.authHeader = new AuthenticationHeader { ApiToken = "WRONG-TOKEN", ClientId = "test" };
            svc.CreateOrder(new Order());
        }

        [TestMethod]
        [ExpectedException(typeof(SoapException))]
        public void CreateOrder_ThrowsSoapException_WhenOrderIsNull()
        {
            var repo = new Mock<IOrderRepository>();
            var svc = CreateService(repo.Object);
            svc.CreateOrder(null);
        }
    }
}
