using System;
using System.Collections.Generic;
using System.Linq;
using Contoso.OrderManagement.Models;

namespace Contoso.OrderManagement
{
    public class OrderRepository : IOrderRepository
    {
        private static readonly OrderRepository _instance = new OrderRepository();

        public static OrderRepository Instance => _instance;

        private readonly object _syncRoot = new object();
        private readonly List<Order> _orders = new List<Order>();
        private int _nextId = 9;

        private OrderRepository()
        {
        }

        public void Seed()
        {
            lock (_syncRoot)
            {
                if (_orders.Count > 0)
                {
                    return;
                }

                var acme = new Customer
                {
                    CustomerId = 1,
                    CompanyName = "Acme Corp",
                    ContactName = "Alicia Monroe",
                    ShippingAddress = new Address
                    {
                        Street = "100 Market Street",
                        City = "Seattle",
                        StateProvince = "WA",
                        PostalCode = "98101",
                        Country = "USA"
                    }
                };

                var northwind = new Customer
                {
                    CustomerId = 2,
                    CompanyName = "Northwind Ltd",
                    ContactName = "James Porter",
                    ShippingAddress = new Address
                    {
                        Street = "42 Harbor Way",
                        City = "Portland",
                        StateProvince = "OR",
                        PostalCode = "97204",
                        Country = "USA"
                    }
                };

                var fabrikam = new Customer
                {
                    CustomerId = 3,
                    CompanyName = "Fabrikam Inc",
                    ContactName = "Priya Shah",
                    ShippingAddress = new Address
                    {
                        Street = "800 Innovation Drive",
                        City = "Austin",
                        StateProvince = "TX",
                        PostalCode = "73301",
                        Country = "USA"
                    }
                };

                _orders.AddRange(new[]
                {
                    CreateSeedOrder(
                        1,
                        new DateTime(2023, 3, 15),
                        OrderStatus.Delivered,
                        acme,
                        "Left at front desk.",
                        new DateTime(2023, 3, 18),
                        Item(1, "Surface Hub Camera", "ELEC-1001", ProductCategory.Electronics, 2, 149.99m),
                        Item(2, "Conference Speakerphone", "ELEC-1010", ProductCategory.Electronics, 1, 329.50m)),
                    CreateSeedOrder(
                        2,
                        new DateTime(2023, 8, 22),
                        OrderStatus.Shipped,
                        acme,
                        "Priority shipping requested.",
                        new DateTime(2023, 8, 24),
                        Item(3, "Industrial Label Printer", "IND-2005", ProductCategory.Industrial, 1, 799.00m),
                        Item(4, "Thermal Labels Pack", "OFF-3012", ProductCategory.Office, 5, 24.75m)),
                    CreateSeedOrder(
                        3,
                        new DateTime(2024, 1, 10),
                        OrderStatus.Pending,
                        acme,
                        "Awaiting finance approval.",
                        null,
                        Item(5, "Ergonomic Keyboard", "OFF-1102", ProductCategory.Office, 10, 59.95m),
                        Item(6, "Wireless Mouse", "ELEC-1022", ProductCategory.Electronics, 10, 34.99m)),
                    CreateSeedOrder(
                        4,
                        new DateTime(2024, 2, 5),
                        OrderStatus.Processing,
                        northwind,
                        "Split shipment allowed.",
                        null,
                        Item(7, "Warehouse Scanner", "IND-2100", ProductCategory.Industrial, 3, 449.00m),
                        Item(8, "Safety Vest", "IND-2105", ProductCategory.Industrial, 12, 18.95m)),
                    CreateSeedOrder(
                        5,
                        new DateTime(2023, 11, 30),
                        OrderStatus.Cancelled,
                        northwind,
                        "Cancelled by customer due to budget hold.",
                        null,
                        Item(9, "Document Shredder", "OFF-1250", ProductCategory.Office, 2, 189.00m)),
                    CreateSeedOrder(
                        6,
                        new DateTime(2023, 6, 14),
                        OrderStatus.Delivered,
                        northwind,
                        "Install in receiving office.",
                        new DateTime(2023, 6, 17),
                        Item(10, "Network Rack UPS", "ELEC-1450", ProductCategory.Electronics, 1, 1249.00m),
                        Item(11, "Rack Mount Kit", "ELEC-1451", ProductCategory.Electronics, 2, 74.95m),
                        Item(12, "Cable Organizer", "OFF-3350", ProductCategory.Office, 6, 12.50m)),
                    CreateSeedOrder(
                        7,
                        new DateTime(2024, 3, 1),
                        OrderStatus.Pending,
                        fabrikam,
                        "Requested consolidated invoice.",
                        null,
                        Item(13, "Portable Projector", "ELEC-1600", ProductCategory.Electronics, 2, 699.00m),
                        Item(14, "Projection Screen", "OFF-3401", ProductCategory.Office, 2, 189.99m)),
                    CreateSeedOrder(
                        8,
                        new DateTime(2024, 3, 18),
                        OrderStatus.Shipped,
                        fabrikam,
                        "Ship to secondary dock.",
                        new DateTime(2024, 3, 20),
                        Item(15, "Packaging Tape Case", "IND-2201", ProductCategory.Industrial, 4, 41.25m),
                        Item(16, "Desktop Monitor 27\"", "ELEC-1700", ProductCategory.Electronics, 6, 239.99m),
                        Item(17, "HDMI Cable", "ELEC-1701", ProductCategory.Electronics, 12, 14.99m))
                });
            }
        }

        public Order GetById(int orderId)
        {
            lock (_syncRoot)
            {
                return _orders.FirstOrDefault(o => o.OrderId == orderId);
            }
        }

        public OrderSummary[] GetByCustomer(int customerId)
        {
            lock (_syncRoot)
            {
                return _orders
                    .Where(o => o.Customer.CustomerId == customerId)
                    .Select(ToSummary)
                    .ToArray();
            }
        }

        public OrderSummary[] Search(OrderSearchCriteria criteria)
        {
            lock (_syncRoot)
            {
                IEnumerable<Order> query = _orders;

                if (criteria != null)
                {
                    if (!string.IsNullOrEmpty(criteria.CustomerName))
                    {
                        query = query.Where(o => o.Customer.CompanyName.IndexOf(criteria.CustomerName, StringComparison.OrdinalIgnoreCase) >= 0);
                    }

                    if (criteria.StatusSpecified)
                    {
                        query = query.Where(o => o.Status == criteria.Status);
                    }

                    if (criteria.FromDateSpecified)
                    {
                        query = query.Where(o => o.OrderDate >= criteria.FromDate);
                    }

                    if (criteria.ToDateSpecified)
                    {
                        query = query.Where(o => o.OrderDate <= criteria.ToDate);
                    }
                }

                return query.Select(ToSummary).ToArray();
            }
        }

        public int Create(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            lock (_syncRoot)
            {
                order.OrderId = _nextId++;
                _orders.Add(order);
                return order.OrderId;
            }
        }

        public void UpdateStatus(int orderId, OrderStatus newStatus)
        {
            lock (_syncRoot)
            {
                var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order == null)
                {
                    throw new ArgumentException($"Order {orderId} not found.");
                }

                order.Status = newStatus;
                if (newStatus == OrderStatus.Shipped || newStatus == OrderStatus.Delivered)
                {
                    if (!order.ShippedDateSpecified)
                    {
                        order.ShippedDate = DateTime.UtcNow;
                        order.ShippedDateSpecified = true;
                    }
                }
                else
                {
                    order.ShippedDate = default(DateTime);
                    order.ShippedDateSpecified = false;
                }
            }
        }

        public void Delete(int orderId)
        {
            lock (_syncRoot)
            {
                var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
                if (order == null)
                {
                    throw new ArgumentException($"Order {orderId} not found.");
                }

                if (order.Status != OrderStatus.Pending)
                {
                    throw new InvalidOperationException($"Only Pending orders can be deleted. Order {orderId} is {order.Status}.");
                }

                _orders.Remove(order);
            }
        }

        private static Order CreateSeedOrder(int orderId, DateTime orderDate, OrderStatus status, Customer customer, string notes, DateTime? shippedDate, params OrderItem[] items)
        {
            return new Order
            {
                OrderId = orderId,
                OrderDate = orderDate,
                Status = status,
                Customer = CloneCustomer(customer),
                Items = items,
                Notes = notes,
                ShippedDate = shippedDate.GetValueOrDefault(),
                ShippedDateSpecified = shippedDate.HasValue
            };
        }

        private static Customer CloneCustomer(Customer customer)
        {
            return new Customer
            {
                CustomerId = customer.CustomerId,
                CompanyName = customer.CompanyName,
                ContactName = customer.ContactName,
                ShippingAddress = new Address
                {
                    Street = customer.ShippingAddress.Street,
                    City = customer.ShippingAddress.City,
                    StateProvince = customer.ShippingAddress.StateProvince,
                    PostalCode = customer.ShippingAddress.PostalCode,
                    Country = customer.ShippingAddress.Country
                }
            };
        }

        private static OrderItem Item(int id, string name, string sku, ProductCategory category, int quantity, decimal unitPrice)
        {
            return new OrderItem
            {
                OrderItemId = id,
                ProductName = name,
                Sku = sku,
                Category = category,
                Quantity = quantity,
                UnitPrice = unitPrice
            };
        }

        private static OrderSummary ToSummary(Order order)
        {
            return new OrderSummary
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                Status = order.Status,
                CustomerName = order.Customer?.CompanyName,
                ItemCount = order.Items?.Length ?? 0,
                TotalAmount = order.Items?.Sum(i => i.Quantity * i.UnitPrice) ?? 0m
            };
        }
    }
}
