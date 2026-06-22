using System.Web.Services.Protocols;
using Contoso.OrderManagement;
using Contoso.OrderManagement.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Contoso.OrderManagement.Tests
{
    [TestClass]
    public class GetOrderByIdTests
    {
        [TestMethod]
        public void GetOrderById_ReturnsOrder_WhenFound()
        {
            var repo = new Mock<IOrderRepository>();
            var expected = new Order { OrderId = 1, Status = OrderStatus.Delivered };
            repo.Setup(r => r.GetById(1)).Returns(expected);
            var svc = new OrderService(repo.Object);
            var result = svc.GetOrderById(1);
            Assert.AreEqual(1, result.OrderId);
        }

        [TestMethod]
        [ExpectedException(typeof(SoapException))]
        public void GetOrderById_ThrowsSoapException_WhenNotFound()
        {
            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.GetById(99)).Returns((Order)null);
            var svc = new OrderService(repo.Object);
            svc.GetOrderById(99);
        }
    }
}
