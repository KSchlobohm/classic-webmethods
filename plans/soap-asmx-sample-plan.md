# SOAP Sample Application — Plan
## ASMX-Style Services on ASP.NET Framework 4.7.2

> **⚠️ IMPORTANT: This repo is intentionally ASMX, NOT WCF.**
>
> Any reference to `[ServiceContract]` and `[OperationContract]` describes **WCF attributes**. This repository deliberately uses the **ASMX programming model** (`[WebService]`, `[WebMethod]`, `.asmx` files, `SoapHttpClientProtocol` proxies). A separate repository covers WCF-specific upgrade challenges. Do not conflate the two — they have distinct upgrade paths, different serializers, and different migration tooling.

---

## Objective

Build a **minimal ASMX web service sample** that covers the 20% of ASMX features found in 80% or more of real-world SOAP web applications.

Goal is **authentic representation of a typical enterprise ASMX app**:
- Demonstrate the patterns most commonly found when surveying real legacy SOAP services
- Provide a realistic, runnable sample that a .NET upgrade tool can analyze
- Avoid rare or niche patterns that inflate scope without adding representational value

---

## ASMX Attribute Mapping Note

Requirements documents and tooling often use WCF terminology. Here is the explicit ASMX equivalent for each:

| WCF / Requirements Term | This Repo Uses (ASMX) |
|-------------------------|-----------------------|
| `[ServiceContract]` | `[WebService(Namespace="...")]` |
| `[OperationContract]` | `[WebMethod]` |
| `[DataContract]` / `[DataMember]` | Plain class + `XmlSerializer` |
| `FaultContract` | `SoapException` with detail XML |
| `BasicHttpBinding` config | `<webServices>` in `web.config` |
| `ServiceHost` (self-hosted) | IIS/IIS Express + `.asmx` handler |
| `svcutil.exe` proxy | `wsdl.exe` / Add Web Reference |

---

## Solution Shape

```
ContosoOrders.sln
├── ContosoOrders.Models          ← Class library (shared DTOs, SOAP header type)
│   ├── Models/Order.cs
│   ├── Models/OrderItem.cs
│   ├── Models/Customer.cs
│   ├── Models/Address.cs
│   ├── Models/OrderSummary.cs
│   ├── Models/OrderSearchCriteria.cs
│   ├── Models/Enums.cs            (OrderStatus, ProductCategory)
│   └── Models/AuthenticationHeader.cs  (SoapHeader subclass)
│
├── ContosoOrders.Service         ← ASP.NET Web App (.NET 4.7.2, ASMX)
│   ├── OrderService.asmx
│   ├── OrderService.asmx.cs
│   ├── App_Data/                  (empty; created at runtime for logs)
│   └── web.config
│
├── ContosoOrders.WebClient       ← ASP.NET Web App (consumer)
│   ├── Default.aspx              (demo page — invokes OrderService)
│   ├── App_WebReferences/
│   │   └── OrderServiceRef/
│   │       ├── Reference.cs      (wsdl.exe-generated SoapHttpClientProtocol proxy)
│   │       ├── OrderService.wsdl
│   │       └── OrderService.disco
│   └── web.config
│
└── ContosoOrders.Tests           ← MSTest unit test project (.NET 4.7.2)
    ├── GetOrderByIdTests.cs
    ├── CreateOrderTests.cs
    ├── UpdateOrderStatusTests.cs
    ├── DeleteOrderTests.cs
    ├── SearchOrdersTests.cs
    └── GetOrdersByCustomerTests.cs
```

---

## Tier 1 — Required Features (ASMX Implementations)

### 1. Service Contract & Operations → `[WebService]` + `[WebMethod]`

The ASMX equivalent of `[ServiceContract]`/`[OperationContract]`.

```csharp
[WebService(
    Namespace   = "http://schemas.contoso.com/orders/2024/",
    Name        = "OrderManagementService",
    Description = "CRUD operations for the Contoso order management system.")]
[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
public class OrderService : WebService
{
    public AuthenticationHeader authHeader;

    [WebMethod(Description = "Gets complete Order by ID.", CacheDuration = 30)]
    public Order GetOrderById(int orderId) { ... }

    [WebMethod(Description = "Returns order summaries for a given customer.")]
    public OrderSummary[] GetOrdersByCustomer(int customerId) { ... }

    [WebMethod(Description = "Searches orders by status, date range, and/or customer name.")]
    public OrderSummary[] SearchOrders(OrderSearchCriteria criteria) { ... }

    [WebMethod(Description = "Creates a new order. Returns new OrderId.")]
    [SoapHeader("authHeader", Required = true)]
    public int CreateOrder(Order order) { ... }

    [WebMethod(Description = "Updates order status.")]
    [SoapHeader("authHeader", Required = true)]
    public void UpdateOrderStatus(int orderId, OrderStatus newStatus) { ... }

    [WebMethod(Description = "Deletes a Pending order.")]
    [SoapHeader("authHeader", Required = true)]
    public void DeleteOrder(int orderId) { ... }
}
```

**Operations:** GetOrderById, GetOrdersByCustomer, SearchOrders, CreateOrder, UpdateOrderStatus, DeleteOrder

---

### 2. Complex Data Contracts → `XmlSerializer` Types

ASMX uses `XmlSerializer` (not `DataContractSerializer`). Complex types are plain classes — no `[DataContract]` needed.

**Key patterns to exercise:**
- Nested types: `Order → OrderItem[]`, `Order → Customer → Address`
- Collections: `[XmlArray("OrderItems")] [XmlArrayItem("Item")] public OrderItem[] Items`
- Enums: `OrderStatus` with `[XmlEnum("Shipped")]`
- Nullable DateTime: the "Specified" boolean flag pattern (`ShippedDateSpecified`)
- Optional strings: `[XmlElement(IsNullable = true)]`
- `[XmlIgnore]` for fields that should not go over the wire

---

### 3. Configuration-Driven Endpoints → `web.config <webServices>`

The ASMX equivalent of `BasicHttpBinding` config. No `<system.serviceModel>` needed — ASMX uses `<system.web>`.

```xml
<system.web>
  <webServices>
    <protocols>
      <add name="HttpSoap"    />   <!-- SOAP 1.1 -->
      <add name="HttpSoap12"  />   <!-- SOAP 1.2 -->
      <add name="Documentation" /> <!-- WSDL at ?wsdl — remove in production -->
    </protocols>
  </webServices>
  <compilation debug="true" targetFramework="4.7.2" />
  <httpRuntime targetFramework="4.7.2" maxRequestLength="65536" executionTimeout="120" />
</system.web>
```

Service URL externalized in client `web.config`:
```xml
<appSettings>
  <add key="OrderServiceUrl" value="http://localhost:54321/OrderService.asmx" />
</appSettings>
```

---

### 4. Client + Server Interaction → Generated `SoapHttpClientProtocol` Proxy

**How to generate the proxy (Visual Studio):**
> Right-click `ContosoOrders.WebClient` → **Add** → **Service Reference** → **Advanced** → **Add Web Reference** → Enter `http://localhost:54321/OrderService.asmx?wsdl`

**Or command line:**
```cmd
wsdl.exe /language:CS /namespace:Contoso.OrderClient /out:Reference.cs ^
    http://localhost:54321/OrderService.asmx?wsdl
```

The generated proxy inherits `SoapHttpClientProtocol` — **NOT** `ClientBase<T>` (that is WCF).

```csharp
// Client usage (Default.aspx.cs)
var proxy = new OrderServiceRef.OrderManagementService();
proxy.Url = ConfigurationManager.AppSettings["OrderServiceUrl"];
proxy.AuthenticationHeaderValue = new OrderServiceRef.AuthenticationHeader
{
    ApiToken = "DEMO-TOKEN-12345",
    ClientId = "web-client"
};
Order o = proxy.GetOrderById(1);
```

---

### 5. Hosting Model → IIS Express (`.asmx` HTTP handler)

ASMX services are hosted by the ASP.NET runtime via the built-in `WebServiceHandlerFactory`. No `ServiceHost` code is needed — that is WCF. IIS Express handles this automatically for `.asmx` files in a Web Application project.

The `.asmx` handler is registered in `Machine.config` and activated by the `<%@ WebService %>` directive:
```aspx
<%@ WebService Language="C#" CodeBehind="OrderService.asmx.cs"
    Class="Contoso.OrderManagement.OrderService" %>
```

---

## Tier 2 — Supporting Features

### 6. Authentication → Custom SOAP Header Token

The most prevalent auth pattern in enterprise ASMX apps: a token transmitted **inside the SOAP envelope**, not in HTTP headers.

```csharp
public AuthenticationHeader authHeader;  // public field on service class

[WebMethod]
[SoapHeader("authHeader", Required = true)]
public int CreateOrder(Order order)
{
    if (authHeader?.ApiToken != "DEMO-TOKEN-12345")
        throw new SoapException("Unauthorized", SoapException.ClientFaultCode);
    // ...
}
```

`AuthenticationHeader` is a subclass of `SoapHeader` defined in `ContosoOrders.Models`:
```csharp
public class AuthenticationHeader : SoapHeader
{
    public string ApiToken { get; set; }
    public string ClientId { get; set; }
}
```

**Client proxy usage:**
```csharp
proxy.AuthenticationHeaderValue = new OrderServiceRef.AuthenticationHeader
{
    ApiToken = "DEMO-TOKEN-12345",
    ClientId = "web-client"
};
```

---

### 7. Fault Handling → `SoapException` with structured `<detail>`

```csharp
throw new SoapException(
    "Order not found.",
    SoapException.ClientFaultCode,         // "Client" fault code
    Context.Request.Url.AbsoluteUri,       // actor
    BuildFaultDetail("ORDER_NOT_FOUND"));   // structured XML detail node
```

This produces a SOAP `<Fault>` envelope with a typed error code in the `<detail>` element.

---

## ❌ Explicitly Out of Scope

| Feature | Why Excluded |
|---------|-------------|
| NetTcpBinding / named pipes | ASMX is HTTP-only |
| WS-* specs (WS-Security, WS-Transaction) | Not applicable to ASMX |
| Duplex callbacks | Not supported in ASMX |
| Custom `IServiceBehavior` pipelines | WCF concept only |
| MTOM | Not exercised |
| `[SoapDocumentMethod]` per-method overrides | Rarely used in real codebases |
| `[XmlInclude]` / `[SoapInclude]` polymorphism | Rare; complicates XmlSerializer with low real-world prevalence |
| `SoapExtension` logging | Niche infrastructure pattern; not found in most ASMX apps |
| Windows Authentication / HTTP Basic Auth | Less universal than SOAP header token for a single-service sample |
| `EnableSession = true` | Not meaningful beyond a demo stub |
| COM+ transactions (`TransactionOption`) | `System.EnterpriseServices` gone in .NET Core |
| **WCF (`[ServiceContract]`, `ServiceHost`)** | **This is a separate repository. See note at top.** |

---

## Success Criteria

The sample is representative when a .NET upgrade tool can:

- [ ] Detect `[WebService]` / `[WebMethod]` service surface area
- [ ] Analyze `web.config <webServices>` protocol and endpoint configuration
- [ ] Interpret XmlSerializer-based data contracts and serialization attributes
- [ ] Reason about the IIS `.asmx` hosting model
- [ ] Identify SOAP header auth patterns (`SoapHeader` subclass)
- [ ] Identify `SoapException` fault handling patterns

---

## Domain: Order Management System

**Why this domain?**

| Requirement | Coverage |
|-------------|---------|
| 2–3 meaningful operations | 6 operations: GetOrderById, GetOrdersByCustomer, SearchOrders, CreateOrder, UpdateOrderStatus, DeleteOrder |
| Complex nested DTOs | Order → OrderItem[]; Order → Customer → Address |
| Collections | `OrderSummary[]`, `OrderItem[]` |
| Enums | `OrderStatus` (Pending/Processing/Shipped/Delivered/Cancelled), `ProductCategory` |
| Date types | `OrderDate`, nullable `ShippedDate` via Specified pattern |
| Auth header | `AuthenticationHeader : SoapHeader` on all write operations |
| Fault handling | Business rule violations, not-found, validation errors |

---

## References

- ASMX `[WebService]` docs: https://learn.microsoft.com/en-us/dotnet/api/system.web.services.webserviceattribute?view=netframework-4.8
- ASMX `[WebMethod]` docs: https://learn.microsoft.com/en-us/dotnet/api/system.web.services.webmethodattribute?view=netframework-4.8
- `SoapHeader` auth pattern: https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/9z52by6a(v=vs.100)
- `SoapException` docs: https://learn.microsoft.com/en-us/dotnet/api/system.web.services.protocols.soapexception?view=netframework-4.8

---

## Repository / Data Layer

`ContosoOrders.Service` uses an in-memory singleton repository (`OrderRepository`) shared across all service requests. There is no database dependency.

### `IOrderRepository` interface (in `ContosoOrders.Service`)

```csharp
public interface IOrderRepository
{
    Order GetById(int orderId);
    OrderSummary[] GetByCustomer(int customerId);
    OrderSummary[] Search(OrderSearchCriteria criteria);
    int Create(Order order);
    void UpdateStatus(int orderId, OrderStatus newStatus);
    void Delete(int orderId);
}
```

### Seed Data

Pre-populated at application startup with **8 orders across 3 customers**:

| OrderId | Customer | Status | OrderDate |
|---------|----------|--------|-----------|
| 1 | Acme Corp (CustomerId=1) | Delivered | 2023-03-15 |
| 2 | Acme Corp | Shipped | 2023-08-22 |
| 3 | Acme Corp | Pending | 2024-01-10 |
| 4 | Northwind Ltd (CustomerId=2) | Processing | 2024-02-05 |
| 5 | Northwind Ltd | Cancelled | 2023-11-30 |
| 6 | Northwind Ltd | Delivered | 2023-06-14 |
| 7 | Fabrikam Inc (CustomerId=3) | Pending | 2024-03-01 |
| 8 | Fabrikam Inc | Shipped | 2024-03-18 |

Orders 3 and 7 (Pending) are the only ones eligible for `DeleteOrder`.

---

## Model Field Definitions

### `Order`
```csharp
public class Order
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime ShippedDate { get; set; }
    public bool ShippedDateSpecified { get; set; }   // XmlSerializer nullable pattern
    public OrderStatus Status { get; set; }
    public Customer Customer { get; set; }
    [XmlArray("OrderItems")]
    [XmlArrayItem("Item")]
    public OrderItem[] Items { get; set; }
    [XmlElement(IsNullable = true)]
    public string Notes { get; set; }
}
```

### `OrderItem`
```csharp
public class OrderItem
{
    public int OrderItemId { get; set; }
    public string ProductName { get; set; }
    public string Sku { get; set; }
    public ProductCategory Category { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

### `Customer`
```csharp
public class Customer
{
    public int CustomerId { get; set; }
    public string CompanyName { get; set; }
    public string ContactName { get; set; }
    public Address ShippingAddress { get; set; }
}
```

### `Address`
```csharp
public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
    public string StateProvince { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }
}
```

### `OrderSummary`
```csharp
public class OrderSummary
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    public string CustomerName { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalAmount { get; set; }
}
```

### `OrderSearchCriteria`
```csharp
public class OrderSearchCriteria
{
    [XmlElement(IsNullable = true)]
    public string CustomerName { get; set; }          // partial match
    public OrderStatus Status { get; set; }
    public bool StatusSpecified { get; set; }         // omit filter if false
    public DateTime FromDate { get; set; }
    public bool FromDateSpecified { get; set; }
    public DateTime ToDate { get; set; }
    public bool ToDateSpecified { get; set; }
}
```

### `Enums`
```csharp
public enum OrderStatus
{
    [XmlEnum("Pending")]    Pending,
    [XmlEnum("Processing")] Processing,
    [XmlEnum("Shipped")]    Shipped,
    [XmlEnum("Delivered")]  Delivered,
    [XmlEnum("Cancelled")]  Cancelled
}

public enum ProductCategory
{
    [XmlEnum("Electronics")] Electronics,
    [XmlEnum("Office")]      Office,
    [XmlEnum("Industrial")]  Industrial,
    [XmlEnum("Other")]       Other
}
```

### `AuthenticationHeader`
```csharp
public class AuthenticationHeader : SoapHeader
{
    public string ApiToken { get; set; }
    public string ClientId { get; set; }
}
```

---

## Unit Tests (`ContosoOrders.Tests`)

**Framework:** MSTest (`[TestClass]` / `[TestMethod]`) — .NET Framework 4.7.2 test project.  
**Mocking:** Moq — mocks `IOrderRepository` so tests do not depend on seed data.  
**Scope:** Service method behavior — happy path + key error/boundary cases.

### Test classes and scenarios

| Test Class | Scenarios |
|------------|-----------|
| `GetOrderByIdTests` | Returns correct order; throws `SoapException` (CLIENT) for unknown ID |
| `GetOrdersByCustomerTests` | Returns summaries for known customer; returns empty array for unknown customer |
| `SearchOrdersTests` | Returns all when no criteria; filters by status; filters by date range; filters by customer name; combined criteria |
| `CreateOrderTests` | Returns new ID; validates token (throws on bad token); throws on null order |
| `UpdateOrderStatusTests` | Updates status; validates token; throws on unknown order ID |
| `DeleteOrderTests` | Deletes a Pending order; validates token; throws on non-Pending order; throws on unknown order ID |
