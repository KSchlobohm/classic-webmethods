using System;
using Contoso.OrderManagement;
using Contoso.OrderManagement.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Contoso.OrderManagement.Tests
{
    [TestClass]
    public class SearchOrdersTests
    {
        private static OrderSummary[] AllOrders()
        {
            return new[]
            {
                new OrderSummary { OrderId = 1, CustomerName = "Acme Corp", Status = OrderStatus.Delivered },
                new OrderSummary { OrderId = 3, CustomerName = "Acme Corp", Status = OrderStatus.Pending },
                new OrderSummary { OrderId = 4, CustomerName = "Northwind Ltd", Status = OrderStatus.Processing }
            };
        }

        [TestMethod]
        public void SearchOrders_ReturnsAll_WhenCriteriaIsNull()
        {
            var repo = new Mock<IOrderRepository>();
            repo.Setup(r => r.Search(null)).Returns(AllOrders());
            var svc = new OrderService(repo.Object);
            var result = svc.SearchOrders(null);
            Assert.AreEqual(3, result.Length);
        }

        [TestMethod]
        public void SearchOrders_ReturnsAll_WhenNoSpecifiedFlags()
        {
            var repo = new Mock<IOrderRepository>();
            var criteria = new OrderSearchCriteria();
            repo.Setup(r => r.Search(criteria)).Returns(AllOrders());
            var svc = new OrderService(repo.Object);
            var result = svc.SearchOrders(criteria);
            Assert.AreEqual(3, result.Length);
        }

        [TestMethod]
        public void SearchOrders_FiltersByStatus()
        {
            var repo = new Mock<IOrderRepository>();
            var criteria = new OrderSearchCriteria { Status = OrderStatus.Pending, StatusSpecified = true };
            repo.Setup(r => r.Search(criteria)).Returns(new[] { new OrderSummary { OrderId = 3, Status = OrderStatus.Pending } });
            var svc = new OrderService(repo.Object);
            var result = svc.SearchOrders(criteria);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(OrderStatus.Pending, result[0].Status);
        }

        [TestMethod]
        public void SearchOrders_FiltersByDateRange()
        {
            var repo = new Mock<IOrderRepository>();
            var criteria = new OrderSearchCriteria
            {
                FromDate = new DateTime(2024, 1, 1),
                FromDateSpecified = true,
                ToDate = new DateTime(2024, 12, 31),
                ToDateSpecified = true
            };
            repo.Setup(r => r.Search(criteria)).Returns(new[] { new OrderSummary { OrderId = 3 }, new OrderSummary { OrderId = 4 } });
            var svc = new OrderService(repo.Object);
            var result = svc.SearchOrders(criteria);
            Assert.AreEqual(2, result.Length);
        }

        [TestMethod]
        public void SearchOrders_FiltersByCustomerName()
        {
            var repo = new Mock<IOrderRepository>();
            var criteria = new OrderSearchCriteria { CustomerName = "Acme" };
            repo.Setup(r => r.Search(criteria)).Returns(new[] { new OrderSummary { OrderId = 1 }, new OrderSummary { OrderId = 3 } });
            var svc = new OrderService(repo.Object);
            var result = svc.SearchOrders(criteria);
            Assert.AreEqual(2, result.Length);
        }

        [TestMethod]
        public void SearchOrders_CombinedCriteria_ReturnsMatchingOrders()
        {
            var repo = new Mock<IOrderRepository>();
            var criteria = new OrderSearchCriteria
            {
                CustomerName = "Acme",
                Status = OrderStatus.Pending,
                StatusSpecified = true
            };
            repo.Setup(r => r.Search(criteria)).Returns(new[] { new OrderSummary { OrderId = 3, CustomerName = "Acme Corp", Status = OrderStatus.Pending } });
            var svc = new OrderService(repo.Object);
            var result = svc.SearchOrders(criteria);
            Assert.AreEqual(1, result.Length);
        }
    }
}
