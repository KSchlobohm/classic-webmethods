# SOAP Sample Application — Plan
## ASMX-Style Services on ASP.NET Framework 4.7.2

> **⚠️ IMPORTANT: This repo is intentionally ASMX, NOT WCF.**
>
> The feature requirements document (below) references `[ServiceContract]` and `[OperationContract]` — those are **WCF attributes**. This repository deliberately uses the **ASMX programming model** (`[WebService]`, `[WebMethod]`, `.asmx` files, `SoapHttpClientProtocol` proxies). A separate repository will cover WCF-specific upgrade challenges. Do not conflate the two — they have distinct upgrade paths, different serializers, and different migration tooling. The "after upgrade" `UpgradeTarget` project uses SoapCore (which does use `[ServiceContract]`/`[OperationContract]`) because that is the ASMX → ASP.NET Core migration target.

---

## Objective

Build a **minimal ASMX web service sample** that demonstrates the highest-value 20% of features representing ~80% of real-world upgrade complexity for the .NET Upgrade Tool.

Goal is **maximum upgrade signal density**:
- Highlight key modernization challenges for ASMX services
- Enable clear before/after comparison (ASMX → SoapCore)
- Provide a repeatable demo and validation scenario

---

## ASMX Attribute Mapping Note

The connected requirements use WCF terminology. Here is the explicit ASMX equivalent for each:

| Requirements Doc Term | This Repo Uses (ASMX) | "After Upgrade" Uses (SoapCore) |
|-----------------------|-----------------------|----------------------------------|
| `[ServiceContract]` | `[WebService(Namespace="...")]` | `[ServiceContract(Namespace="...")]` |
| `[OperationContract]` | `[WebMethod]` | `[OperationContract]` |
| `[DataContract]` / `[DataMember]` | Plain class + `XmlSerializer` | `[DataContract]` / `[DataMember]` |
| `FaultContract` | `SoapException` with detail XML | `FaultException<T>` with `[FaultContract]` |
| `BasicHttpBinding` config | `<webServices>` in `web.config` | SoapCore endpoint config in `Program.cs` |
| `ServiceHost` (self-hosted) | IIS/IIS Express + `.asmx` handler | `app.UseEndpoints(e => e.UseSoapEndpoint<...>())` |
| `svcutil.exe` proxy | `wsdl.exe` / Add Web Reference | Same wsdl.exe or re-generated via svcutil |

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
├── ContosoOrders.Service         ← ASP.NET Web App (ASMX) — three auth surfaces
│   ├── OrderService.asmx              ← Auth: Custom SOAP header token
│   ├── OrderService.asmx.cs
│   ├── OrderServiceWindows.asmx       ← Auth: Windows Authentication (NTLM/Kerberos)
│   ├── OrderServiceWindows.asmx.cs
│   ├── OrderServiceBasic.asmx         ← Auth: HTTP Basic Auth [⚠️ see note below]
│   ├── OrderServiceBasic.asmx.cs
│   ├── Extensions/
│   │   └── LoggingExtension.cs        (SoapExtension — request/response logging)
│   └── web.config                     (<location> blocks per service file)
│
├── ContosoOrders.WebClient       ← ASP.NET Web App (consumer)
│   ├── Default.aspx              (simple invocation demo page)
│   ├── App_WebReferences/
│   │   └── OrderServiceRef/
│   │       ├── Reference.cs      (wsdl.exe-generated SoapHttpClientProtocol proxy)
│   │       ├── OrderService.wsdl
│   │       └── OrderService.disco
│   └── web.config
│
└── ContosoOrders.UpgradeTarget   ← ASP.NET Core (post-upgrade, SoapCore)
    ├── Program.cs                (UseSoapEndpoint<OrderService>)
    ├── IOrderService.cs          ([ServiceContract] interface)
    ├── OrderService.cs           (implementation, same logic)
    └── appsettings.json
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
    [WebMethod(Description = "Gets complete Order by ID.", CacheDuration = 30)]
    public Order GetOrderById(int orderId) { ... }

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

**Minimum operations (2–3 meaningful ops):** ✅ GetOrderById, CreateOrder, UpdateOrderStatus, DeleteOrder, SearchOrders

---

### 2. Complex Data Contracts → `XmlSerializer` Types

ASMX uses `XmlSerializer` (not `DataContractSerializer`). Complex types are plain classes — no `[DataContract]` needed.

**Key patterns to exercise:**
- Nested types: `Order → OrderItem[] → Product`, `Order → Customer → Address`
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

## Tier 2 — Supporting Features (Optional)

### 6. Authentication — All Three Mechanisms

Three separate `.asmx` service files demonstrate three distinct auth patterns, each with a different upgrade challenge. `web.config` uses `<location>` blocks to apply different IIS auth settings per path.

---

#### 6a. Custom SOAP Header Token (`OrderService.asmx`)

Token travels **inside the SOAP envelope** — not in HTTP headers.

```xml
<!-- web.config -->
<location path="OrderService.asmx">
  <system.web>
    <authentication mode="None" />
    <authorization><allow users="*" /></authorization>
  </system.web>
</location>
```

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

**Upgrade challenge:** Token moves from `SoapHeader` subclass to `[MessageContract]`/`[MessageHeader]` in SoapCore — structural refactor of how credentials travel in the envelope.

---

#### 6b. Windows Authentication (`OrderServiceWindows.asmx`)

IIS handles authentication; service reads the caller's identity via `User.Identity`.

```xml
<!-- web.config -->
<location path="OrderServiceWindows.asmx">
  <system.web>
    <authentication mode="Windows" />
    <authorization><deny users="?" /></authorization>
  </system.web>
  <system.webServer>
    <security>
      <authentication>
        <anonymousAuthentication enabled="false" />
        <windowsAuthentication enabled="true" />
      </authentication>
    </security>
  </system.webServer>
</location>
```

```csharp
[WebMethod(Description = "Returns orders for the authenticated Windows user.")]
public Order[] GetMyOrders()
{
    string callerIdentity = User.Identity.Name;  // e.g. "DOMAIN\jsmith"
    return repository.GetByOwner(callerIdentity);
}
```

**✅ Testable with IIS Express** — no full IIS install needed. Requires one manual edit to `.vs\config\applicationhost.config`:

```xml
<!-- Find the <site> entry for ContosoOrders.Service and add: -->
<system.webServer>
  <security>
    <authentication>
      <anonymousAuthentication enabled="false" />
      <windowsAuthentication enabled="true" />
    </authentication>
  </security>
</system.webServer>
```

> **Note:** This edit is per-developer machine. Add a `README-IISExpress-WindowsAuth.md` with setup steps. The `.vs/` folder is typically git-ignored so the change won't persist across clones.

**Upgrade challenge:** `<authentication mode="Windows">` + IIS module dependency disappears in ASP.NET Core. Replacement is `services.AddAuthentication(NegotiateDefaults.AuthenticationScheme).AddNegotiate()` — a pure-code migration with no `web.config` equivalent.

---

#### 6c. HTTP Basic Authentication (`OrderServiceBasic.asmx`)

> **⚠️ PLACEHOLDER — limited local testability without setup**
>
> IIS Express supports Basic Auth via `applicationhost.config` (`<basicAuthentication enabled="true" />`), but it authenticates against **Windows local machine accounts** — not a custom user store. This makes it awkward for a portable demo. Recommended approach: implement the service code and proxy `Credentials` pattern in full; document IIS Express setup steps in a README for manual testing. The upgrade signal is in the code and `web.config` patterns, not the live auth challenge.

```xml
<!-- web.config -->
<location path="OrderServiceBasic.asmx">
  <system.webServer>
    <security>
      <authentication>
        <anonymousAuthentication enabled="false" />
        <basicAuthentication enabled="true" realm="ContosoOrders" />
      </authentication>
    </security>
  </system.webServer>
</location>
```

```csharp
[WebMethod(Description = "Gets order — requires HTTP Basic credentials.")]
public Order GetOrderById(int orderId)
{
    // IIS has already validated credentials before this runs.
    // User.Identity.Name available if needed for authorization logic.
    return repository.GetById(orderId);
}
```

**Client proxy usage:**
```csharp
var proxy = new OrderServiceBasicRef.OrderManagementService();
proxy.Credentials = new NetworkCredential("username", "password");
Order o = proxy.GetOrderById(1);
```

**Upgrade challenge:** `proxy.Credentials` (HTTP-level) maps to ASP.NET Core Basic Auth middleware or SoapCore's `opt.UseBasicAuthentication = true`. The IIS module dependency (`<basicAuthentication>`) has no direct equivalent — replaced by middleware registration in `Program.cs`.

---

### 7. Fault Handling → `SoapException` with structured `<detail>`

The ASMX equivalent of `FaultException<T>` / `[FaultContract]`.

```csharp
throw new SoapException(
    "Order not found.",
    SoapException.ClientFaultCode,         // "Client" fault code
    Context.Request.Url.AbsoluteUri,       // actor
    BuildFaultDetail("ORDER_NOT_FOUND"));   // structured XML detail node
```

This produces a SOAP `<Fault>` envelope — equivalent to WCF's typed fault contract.

---

### 8. Logging / Diagnostics → `SoapExtension`

The ASMX equivalent of WCF `IDispatchMessageInspector`. Intercepts raw SOAP XML before/after deserialization.

```csharp
public class LoggingExtension : SoapExtension
{
    public override void ProcessMessage(SoapMessage message)
    {
        switch (message.Stage)
        {
            case SoapMessageStage.BeforeDeserialize: /* log incoming XML */ break;
            case SoapMessageStage.AfterSerialize:    /* log outgoing XML */ break;
        }
    }
}
```

Register globally in `web.config`:
```xml
<webServices>
  <soapExtensionTypes>
    <add type="Contoso.OrderManagement.LoggingExtension, ContosoOrders.Service"
         priority="1" group="0" />
  </soapExtensionTypes>
</webServices>
```

---

## ❌ Explicitly Out of Scope

| Feature | Why Excluded |
|---------|-------------|
| NetTcpBinding / named pipes | ASMX is HTTP-only; also out of scope per requirements |
| WS-* specs (WS-Security, WS-Transaction) | Not applicable to ASMX; also per requirements |
| Duplex callbacks | Not supported in ASMX; also per requirements |
| Custom `IServiceBehavior` pipelines | WCF concept only; ASMX uses `SoapExtension` |
| MTOM | Not exercised; also per requirements |
| `[SoapDocumentMethod]` per-method overrides | Rarely used; also not supported in SoapCore upgrade target |
| `[XmlInclude]` / `[SoapInclude]` polymorphism | Not supported in SoapCore — would be a dead end |
| `EnableSession = true` beyond demo | Not meaningful for upgrade signal |
| COM+ transactions (`TransactionOption`) | `System.EnterpriseServices` gone in .NET Core |
| **WCF (`[ServiceContract]`, `ServiceHost`)** | **This is a separate repository. See note at top.** |

---

## UpgradeTarget — SoapCore (After Upgrade)

The `ContosoOrders.UpgradeTarget` project demonstrates the **same service** running on ASP.NET Core via SoapCore. This is the "after" side of the before/after comparison.

**Key transformation points:**
| Before (ASMX) | After (SoapCore) |
|---------------|-----------------|
| `[WebService(Namespace="...")]` on class | `[ServiceContract(Namespace="...")]` on **interface** |
| `[WebMethod]` on method | `[OperationContract]` on interface method |
| `SoapHeader` subclass | `[MessageContract]` / `[MessageHeader]` |
| `SoapException` | `FaultException<T>` with `[FaultContract]` |
| `web.config <webServices>` | `Program.cs app.UseEndpoints(...)` |
| `IIS Express + .asmx` | Kestrel + `UseSoapEndpoint<T>` |
| `SoapExtension` | `ISoapMessageProcessor` / `IMessageInspector2` |

**SoapCore limitations to call out explicitly:**
- ❌ `[XmlInclude]` / `[SoapInclude]` — not supported (design around this in the sample)
- ❌ `[SoapDocumentMethod]` — not supported

---

## Success Criteria

The sample is successful when the upgrade tool can:

- [ ] Detect `[WebService]` / `[WebMethod]` service surface area
- [ ] Analyze `web.config <webServices>` protocol and endpoint configuration
- [ ] Interpret XmlSerializer-based data contracts and serialization attributes
- [ ] Reason about the IIS `.asmx` hosting model
- [ ] Identify SOAP header auth patterns (`SoapHeader` subclass)
- [ ] Map `SoapException` fault handling to `FaultException<T>`
- [ ] Support clear before/after comparison: ASMX 4.7.2 → SoapCore on .NET 8

---

## Domain: Order Management System

**Why this domain?**

| Requirement | Coverage |
|-------------|---------|
| 2–3 meaningful operations | 6 operations: GetOrderById, GetOrdersByCustomer, SearchOrders, CreateOrder, UpdateOrderStatus, DeleteOrder |
| Complex nested DTOs | Order → OrderItem[] → Product; Order → Customer → Address |
| Collections | `Order[]`, `OrderSummary[]`, `OrderItem[]` |
| Enums | `OrderStatus` (Pending/Processing/Shipped/Delivered/Cancelled) |
| Date types | `OrderDate`, nullable `ShippedDate` via Specified pattern |
| Auth header | `AuthenticationHeader : SoapHeader` on all write operations |
| Fault handling | Business rule violations, not-found, validation errors |
| Logging hook | `LoggingExtension : SoapExtension` on all requests |

---

## References

- Research report (full detail): `../session-state/.../research/find-all-of-the-important-topics-that-we-would-nee.md`
- SoapCore repo: https://github.com/DigDes/SoapCore
- ASMX `[WebService]` docs: https://learn.microsoft.com/en-us/dotnet/api/system.web.services.webserviceattribute?view=netframework-4.8
- ASMX `[WebMethod]` docs: https://learn.microsoft.com/en-us/dotnet/api/system.web.services.webmethodattribute?view=netframework-4.8
- `SoapHeader` auth pattern: https://learn.microsoft.com/en-us/previous-versions/dotnet/netframework-4.0/9z52by6a(v=vs.100)
- `SoapException` docs: https://learn.microsoft.com/en-us/dotnet/api/system.web.services.protocols.soapexception?view=netframework-4.8
- `SoapExtension` docs: https://learn.microsoft.com/en-us/dotnet/api/system.web.services.protocols.soapextension?view=netframework-4.8
