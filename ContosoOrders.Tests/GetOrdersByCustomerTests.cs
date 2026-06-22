using Contoso.OrderManagement;
using Contoso.OrderManagement.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Contoso.OrderManagement.Tests
{
    [TestClass]
    public class GetOrdersByCustomerTests
    {
        [TestMethod]
        public void GetOrdersByCustomer_ReturnsSummaries_WhenFound()
        {
            var repo = new Mock<IOrderRepository>();
            var summaries = new[] { new OrderSummary { OrderId = 1, CustomerName = "Acme" } };
            repo.Setup(r => r.GetByCustomer(1)).Returns(summaries);
            var svc = new OrderService(repo.Object);
            var result = svc.GetOrdersByCustomer(1);
            Assert.AreEqual(1, result.Length);
        }

        [TestMethod]
        public void GetOrdersByCustomer_ReturnsEmpty_WhenCustomerUnknown()
        {
            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.GetByCustomer(999)).Returns(new OrderSummary[0]);
            var svc = new OrderService(repo.Object);
            var result = svc.GetOrdersByCustomer(999);
            Assert.AreEqual(0, result.Length);
        }
    }
}
