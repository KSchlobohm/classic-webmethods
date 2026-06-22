using Contoso.OrderManagement.Models;

namespace Contoso.OrderManagement
{
    public interface IOrderRepository
    {
        Order GetById(int orderId);
        OrderSummary[] GetByCustomer(int customerId);
        OrderSummary[] Search(OrderSearchCriteria criteria);
        int Create(Order order);
        void UpdateStatus(int orderId, OrderStatus newStatus);
        void Delete(int orderId);
    }
}
