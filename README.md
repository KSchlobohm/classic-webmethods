# tiny-soap — Contoso Orders ASMX Sample

A minimal, runnable **ASMX web service** sample targeting **ASP.NET / .NET Framework 4.7.2**. It exists to give .NET upgrade tooling a realistic, representative legacy SOAP codebase to analyze.

> **⚠️ This is ASMX, not WCF.**  
> The repo intentionally uses `[WebService]` / `[WebMethod]` / `SoapHttpClientProtocol` — the classic ASMX model. WCF (`[ServiceContract]`, `ServiceHost`, `ClientBase<T>`) is a separate concern with a different upgrade path.

---

## Solution Structure

```
ContosoOrders.sln
├── ContosoOrders.Models       Class library — shared DTOs and SOAP header type
├── ContosoOrders.Service      ASP.NET Web App (.NET 4.7.2) — the ASMX service
├── ContosoOrders.WebClient    ASP.NET Web App — ASMX proxy consumer (demo page)
└── ContosoOrders.Tests        MSTest unit test project (.NET 4.7.2)
```

### ContosoOrders.Models
Shared data transfer objects serialized by `XmlSerializer`:

| Type | Purpose |
|------|---------|
| `Order` | Full order with nested `Customer` and `OrderItem[]` |
| `OrderItem` | Line item with SKU, quantity, price, and `ProductCategory` enum |
| `Customer` | Company/contact info with `Address` |
| `OrderSummary` | Lightweight projection returned by list/search operations |
| `OrderSearchCriteria` | Filter parameters with "Specified" boolean flag pattern for optional fields |
| `AuthenticationHeader` | `SoapHeader` subclass — token sent inside the SOAP envelope |
| `OrderStatus` / `ProductCategory` | Enums decorated with `[XmlEnum]` |

### ContosoOrders.Service
The ASMX web service exposing six operations:

| Operation | Auth Required | Description |
|-----------|:---:|-------------|
| `GetOrderById` | — | Returns a full `Order` by ID; 30-second cache |
| `GetOrdersByCustomer` | — | Returns `OrderSummary[]` for a customer |
| `SearchOrders` | — | Filters by status, date range, and/or customer name |
| `CreateOrder` | ✔ | Creates a new order; returns new `OrderId` |
| `UpdateOrderStatus` | ✔ | Transitions order to a new `OrderStatus` |
| `DeleteOrder` | ✔ | Deletes a `Pending` order |

The service uses an **in-memory singleton repository** (`OrderRepository`) seeded with 8 orders across 3 fictional customers at startup — no database dependency.

### ContosoOrders.WebClient
A demo ASP.NET Web Forms app (`Default.aspx`) that consumes the service through a `wsdl.exe`-generated `SoapHttpClientProtocol` proxy (checked in under `App_WebReferences/`). The service URL is read from `appSettings` in `web.config`.

### ContosoOrders.Tests
MSTest (.NET 4.7.2) unit tests that mock `IOrderRepository` with Moq. Tests cover happy-path and error scenarios for all six operations.

---

## Key ASMX Patterns Demonstrated

| Pattern | Implementation |
|---------|---------------|
| Service declaration | `[WebService(Namespace=...)]` + `[WebMethod]` |
| SOAP 1.1 / 1.2 | `<webServices><protocols>` in `web.config` |
| `XmlSerializer` contracts | Nested types, `[XmlArray]`/`[XmlArrayItem]`, `[XmlEnum]`, `[XmlIgnore]`, nullable via `*Specified` flag |
| SOAP header auth | `AuthenticationHeader : SoapHeader`; `[SoapHeader("authHeader", Required=true)]` on write ops |
| Fault handling | `SoapException` with structured `<detail>` XML (CLIENT fault code) |
| Client proxy | `wsdl.exe`-generated `SoapHttpClientProtocol` — not `ClientBase<T>` |
| Testability | Two-constructor pattern so unit tests bypass the ASP.NET runtime |
| Thread safety | Singleton repository guarded by `lock (_syncRoot)` |

---

## Running Locally

Prerequisites: Visual Studio 2022 (with ASP.NET / .NET Framework workload) or MSBuild + IIS Express.

1. Open `ContosoOrders.sln`.
2. Set **ContosoOrders.Service** as the startup project and note the IIS Express port (default `54321`).
3. Start the service.
4. Verify the WSDL: `http://localhost:54321/OrderService.asmx?wsdl`
5. Optionally set **ContosoOrders.WebClient** as a second startup project to run the demo page.

To run tests: `dotnet test` or use the Visual Studio Test Explorer.

---

## Purpose

This repo is designed so that a .NET upgrade tool can:

- Detect `[WebService]` / `[WebMethod]` service surface area
- Analyze `web.config <webServices>` protocol and endpoint configuration
- Interpret `XmlSerializer`-based data contracts and serialization attributes
- Reason about the IIS `.asmx` hosting model
- Identify SOAP header authentication patterns
- Identify `SoapException` fault handling patterns
