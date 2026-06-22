using System.Web.Services.Protocols;
using Contoso.OrderManagement;
using Contoso.OrderManagement.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Contoso.OrderManagement.Tests
{
    [TestClass]
    public class DeleteOrderTests
    {
        private static OrderService CreateService(IOrderRepository repo)
        {
            var svc = new OrderService(repo);
            svc.authHeader = new AuthenticationHeader { ApiToken = "DEMO-TOKEN-12345", ClientId = "test" };
            return svc;
        }

        [TestMethod]
        public void DeleteOrder_Succeeds_ForPendingOrder()
        {
            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.Delete(3));
            var svc = CreateService(repo.Object);
            svc.DeleteOrder(3);
            repo.Verify(r => r.Delete(3), Times.Once);
        }

        [TestMethod]
        [ExpectedException(typeof(SoapException))]
        public void DeleteOrder_ThrowsSoapException_WhenTokenInvalid()
        {
            var repo = new Mock<IOrderRepository>();
            var svc = new OrderService(repo.Object);
            svc.authHeader = new AuthenticationHeader { ApiToken = "BAD-TOKEN" };
            svc.DeleteOrder(3);
        }

        [TestMethod]
        [ExpectedException(typeof(SoapException))]
        public void DeleteOrder_ThrowsSoapException_WhenNotPending()
        {
            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.Delete(1))
                .Throws(new System.InvalidOperationException("Only Pending orders can be deleted."));
            var svc = CreateService(repo.Object);
            svc.DeleteOrder(1);
        }

        [TestMethod]
        [ExpectedException(typeof(SoapException))]
        public void DeleteOrder_ThrowsSoapException_WhenNotFound()
        {
            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.Delete(99))
                .Throws(new System.ArgumentException("Order 99 not found."));
            var svc = CreateService(repo.Object);
            svc.DeleteOrder(99);
        }
    }
}
