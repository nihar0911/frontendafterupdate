# READ-ONLY Comprehensive Frontend Architecture & Procurement Audit
**Project:** Smart Vendor Management System — Frontend (`VendorManagementFrontend`)  
**Audit Type:** Strictly Read-Only Baseline Assessment & Gap Analysis  
**Framework:** ASP.NET Core 9.0 Blazor Web App (Interactive Server Render Mode)  

---

## 1. Frontend Architecture

### 1.1 Project Structure & Directory Layout
The project is built on **ASP.NET Core 9.0** (`net9.0`) using **Blazor Interactive Server** mode configured at the root level (`InteractiveServerRenderMode`). The codebase adheres to a structured, feature-oriented organization:

```
VenodorManagementFrontend/
├── Components/
│   ├── App.razor                      # Root HTML document, meta tags, CSS links, global JS interop scripts
│   ├── Routes.razor                   # Blazor Router, RouteView with MainLayout, FocusOnNavigate
│   ├── _Imports.razor                 # Global namespace imports (Blazor, Models, Services, Helpers, Shared components)
│   ├── Layout/
│   │   ├── MainLayout.razor           # Top-level shell layout wrapping @Body in min-vh-100 container
│   │   ├── MainLayout.razor.css       # Scoped layout CSS
│   │   ├── NavMenu.razor              # Minimal default navigation sidebar
│   │   └── NavMenu.razor.css          # Navigation menu styling
│   ├── Pages/                         # 32 route pages organized into feature folders
│   │   ├── Home/                      # Root "/" and "/login" authentication hub
│   │   ├── Admin/                     # Admin dashboard & management subpages
│   │   ├── AdminOrganizations/        # Organization CRUD
│   │   ├── AdminOutlets/              # Outlet CRUD
│   │   ├── AdminProducts/             # Product master CRUD
│   │   ├── AdminUsers/                # User management CRUD
│   │   ├── AdminVendors/              # Vendor master CRUD
│   │   ├── AdminVendorProducts/       # Vendor-to-Product catalog mapping
│   │   ├── AdminTaxRates/             # Tax rate administration
│   │   ├── AdminContracts/            # Global contract directory
│   │   ├── AdminProfile/              # Admin profile settings
│   │   ├── AdminLogin/                # Dedicated Admin login portal
│   │   ├── OrgDashboard/              # Organization Manager dashboard
│   │   ├── OrgOutlets/                # Organization outlet directory & management
│   │   ├── OrgContracts/              # Organization contract monitoring
│   │   ├── OrgPurchaseOrders/         # Organization purchase order operations
│   │   ├── OrgInvoices/               # 3-Way matching invoice review & approval
│   │   ├── OrgPayments/               # Invoice payment processing & history
│   │   ├── OrgVendorPerformance/      # Vendor performance analytics & reviews
│   │   ├── OrgLogin/                  # Dedicated Organization login portal
│   │   ├── OutletManagerDashboard/    # Outlet Manager dashboard & delivery receipt
│   │   ├── OutletLogin/               # Dedicated Outlet login portal
│   │   ├── VendorDashboard/           # Vendor Manager operations portal
│   │   ├── VendorProcurement/         # Vendor request review & quotation submission
│   │   ├── VendorLogin/               # Dedicated Vendor login portal
│   │   ├── Procurement/               # Purchase request creation & vendor recommendation dispatch
│   │   ├── PurchaseRequestDetails/    # Purchase request single detail view
│   │   ├── QuotationsReview/          # Multi-quotation review and comparison
│   │   ├── QuotationReview/           # Single quotation detail review & acceptance
│   │   ├── CreateContract/            # Contract generation & vendor allocation
│   │   ├── ContractDetails/           # Contract view & reset
│   │   └── Error/                     # Error handling view
│   └── Shared/                        # Reusable modular components
│       ├── LoginForm/                 # Parametric role-branded login form component
│       ├── OutletForm/                # Outlet create/edit modal
│       ├── ProductForm/               # Product create/edit modal
│       ├── UserForm/                  # User create/edit modal
│       ├── VendorForm/                # Vendor create/edit modal
│       └── SubmitReviewModal/         # Goods-received vendor review & rating modal
├── Helpers/
│   └── CurrencyHelper.cs              # Currency formatting utilities (e.g., LKR / Rs. formatting)
├── Models/                            # Strongly-typed domain DTOs, Commands, and API Responses
│   ├── Authentication/
│   ├── Common/
│   ├── Contracts/
│   ├── Deliveries/
│   ├── Invoices/
│   ├── Notifications/
│   ├── Organizations/
│   ├── Outlets/
│   ├── Payments/
│   ├── Procurement/
│   ├── Products/
│   ├── PurchaseOrders/
│   ├── PurchaseRequests/
│   ├── Quotations/
│   ├── TaxRates/
│   ├── Users/
│   ├── VendorFeedback/
│   ├── VendorPerformance/
│   ├── VendorProducts/
│   ├── VendorRecommendations/
│   └── Vendors/
├── Services/                          # Service layer and HTTP API clients
│   ├── ApiService.cs                  # Monolithic typed HTTP API client (1,776 lines)
│   ├── AuthService.cs                 # In-memory scoped authentication and session state manager
│   ├── Infrastructure/                # Base ApiClient helper
│   └── [Domain]/                      # Domain-specific services (Contracts, Invoices, Deliveries, etc.)
└── wwwroot/
    ├── app.css                        # Core Design System (Tokens, Typography, Cards, Tables, Badges)
    ├── lib/bootstrap/                 # Bootstrap base CSS framework
    └── images/                        # Static graphical assets
```

### 1.2 Core Architectural Patterns
1. **Code-Behind Separation**: Every complex page and shared component strictly follows the partial class pattern (`[Name].razor`, `[Name].razor.cs`, and `[Name].razor.css`).
2. **Scoped CSS Isolation**: Each feature page encapsulates its layout, transitions, responsive overrides, and animations in scoped `.razor.css` files.
3. **Consolidated Typed API Service**: `ApiService.cs` acts as the primary typed HTTP client, injected as `@inject ApiService Api` across all pages.
4. **Scoped Authentication State Manager**: `AuthService.cs` is registered as a scoped service (`builder.Services.AddScoped<AuthService>()`), retaining the in-memory `LoginResponse` and emitting `OnAuthStateChanged` events for reactive UI updates.
5. **Client-Side JS Interop**: `App.razor` provides global JavaScript utility functions for downloading base64 documents (`window.downloadFileFromBase64`) and previewing binary PDFs in a new tab (`window.viewPdfFromBase64`).

---

## 2. Authentication & Role Handling

### 2.1 Login Flow
1. **Universal Login Hub (`/` or `/login`)**: `Home.razor` serves as the universal authentication gate. If the user is already authenticated, it displays the active session badge and directs the user to their role-specific dashboard.
2. **Dedicated Role Portals**:
   - Admin Login: `/admin/login` (`AdminLogin.razor`)
   - Organization Login: `/organization/login`, `/org/login` (`OrgLogin.razor`)
   - Outlet Login: `/outlet/login`, `/outlet-manager/login` (`OutletLogin.razor`)
   - Vendor Login: `/vendor/login` (`VendorLogin.razor`)
3. **Execution**:
   - The login component submits `LoginRequest { Email, Password }` to `POST api/auth/login` via `Api.LoginAsync(email, password)`.
   - On success, the returned `LoginResponseWrapper.Login` (`LoginResponse`) is passed to `Auth.SetUser(loginResponse)`.
   - Role matching is validated (if on a dedicated portal), and navigation is triggered.

### 2.2 AuthService & ApiService Responsibilities
- **`AuthService.cs`**:
  - Holds `CurrentUser` (`LoginResponse`).
  - Exposes state properties: `IsAuthenticated`, `Token`, `Role`, `UserName`, `Email`, `UserID`, `OrganizationID`, `OutletID`, `VendorID`.
  - Exposes role check boolean properties:
    - `IsAdmin`: `Role == "Admin"`
    - `IsOrgManager`: `Role == "Organization Manager"`
    - `IsOutletManager`: `Role == "Outlet Manager"`
    - `IsVendorManager`: `Role == "Vendor Manager"`
  - Emits `OnAuthStateChanged` on login or logout.
- **`ApiService.cs`**:
  - Injects `HttpClient` and `AuthService`.
  - Implements `SetAuthHeader()`:
    ```csharp
    if (_authService.IsAuthenticated && !string.IsNullOrEmpty(_authService.Token))
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _authService.Token);
    }
    ```
  - Automatically attaches the Bearer token to all outgoing HTTP requests.

### 2.3 Verified Supported Roles & Post-Login Destinations
Based on a direct code inspection of `AuthService.cs` and `Home.razor.cs`:

| Role Name in Code | AuthService Check | Post-Login Target Route | Dashboard Component |
| :--- | :--- | :--- | :--- |
| **Admin** | `Auth.IsAdmin` | `/admin` | `Components.Pages.Admin.Admin` |
| **Organization Manager** | `Auth.IsOrgManager` | `/org-dashboard` | `Components.Pages.OrgDashboard.OrgDashboard` |
| **Vendor Manager** | `Auth.IsVendorManager` | `/vendor-dashboard` | `Components.Pages.VendorDashboard.VendorDashboard` |
| **Outlet Manager** | `Auth.IsOutletManager` | `/outlet-manager` | `Components.Pages.OutletManagerDashboard.OutletManagerDashboard` |

> [!IMPORTANT]
> **Code Verification Result:** A **Purchase Manager** role does **NOT** exist anywhere in the current frontend codebase. No `IsPurchaseManager` property, no purchase manager route, and no purchase manager login handler exist in `AuthService.cs` or any page component.

---

## 3. Current Role-Specific Frontends

### 3.1 Admin Frontend
- **Dashboard**: `/admin` (`Admin.razor`)
- **Sidebar Navigation**:
  - `Dashboard` (`/admin`)
  - `Organizations` (`/admin/organizations`)
  - `Users` (`/admin/users`)
  - `Vendors` (`/admin/vendors`)
  - `Outlets` (`/admin/outlets`)
  - `Products` (`/admin/products`)
  - `Vendor Products` (`/admin/vendorproducts`)
  - `Tax Rates` (`/admin/taxrates`)
  - `Contracts` (`/admin/contracts`)
  - `Profile` (`/admin/profile`)
- **Main Pages & Actions**:
  - `AdminOrganizations`: Create/Edit Organizations (`GetOrganizationsAsync`, `CreateOrganizationAsync`, `UpdateOrganizationAsync`).
  - `AdminUsers`: Create/Edit Users across any Organization/Outlet/Vendor (`GetUsersAsync`, `CreateUserAsync`, `UpdateUserAsync`).
  - `AdminVendors`: Create/Edit Vendors (`GetVendorsAsync`, `CreateVendorAsync`, `UpdateVendorAsync`).
  - `AdminOutlets`: Create/Edit Outlets across organizations (`GetOutletsAsync`, `CreateOutletAsync`, `UpdateOutletAsync`).
  - `AdminProducts`: Product catalog management (`GetProductsAsync`, `CreateProductAsync`, `UpdateProductAsync`).
  - `AdminVendorProducts`: Map vendor supply capability, price, delivery lead days (`GetVendorProductsAsync`, `CreateVendorProductAsync`, `UpdateVendorProductAsync`, `DeleteVendorProductAsync`).
  - `AdminTaxRates`: Configure tax codes and percentages (`GetTaxRatesAsync`, `CreateTaxRateAsync`, `UpdateTaxRateAsync`).
  - `AdminContracts`: View and monitor all platform contracts globally (`GetContractsAsync`).
- **Classification**: **Operational Master Data & System Administration** + Global Platform Monitoring.

### 3.2 Organization Manager Frontend
- **Dashboard**: `/org-dashboard` (`OrgDashboard.razor`)
- **Sidebar Navigation**:
  - `Dashboard` (`/org-dashboard`)
  - `Outlets` (`/organization/outlets`)
  - `Purchase Requests` (`/procurement`)
  - `Quotations` (`/quotations`)
  - `Contracts` (`/organization/contracts`)
  - `Purchase Orders` (`/organization/purchase-orders`)
  - `Invoices` (`/organization/invoices`)
  - `Payments` (`/organization/payments`)
  - `Vendor Performance` (`/organization/vendor-performance`)
- **Main Pages & Actions**:
  - `OrgDashboard`: Organization KPIs, active contracts, pending quotations, outlet list modal, in-app notifications.
  - `Procurement` (`/procurement`): Step 1 Outlet Selection -> Step 2 Product & Quantity -> Step 3 Vendor Recommendation matrix & direct dispatch (`CreatePurchaseRequestAsync`, `DispatchPurchaseRequestAsync`).
  - `QuotationsReview` (`/quotations`) & `QuotationReview` (`/quotation-review/{id}`): Review submitted quotations, compare pricing/tax/validity, execute `AcceptQuotationAsync` or `RejectQuotationAsync`.
  - `CreateContract` (`/contract/create/{QuotationId}`): Create single or multi-vendor split percentage contracts from accepted quotations (`CreateContractAsync`, `CreateContractFromQuotationAsync`).
  - `OrgPurchaseOrders` (`/organization/purchase-orders`): Create PO from accepted quotation (`CreatePurchaseOrderAsync`), view PO status, monitor delivery records, open feedback modal.
  - `OrgInvoices` (`/organization/invoices`): 3-way match invoice details against PO and delivery records; execute `ApproveInvoiceAsync` or `RejectInvoiceAsync`; download invoice PDFs.
  - `OrgPayments` (`/organization/payments`): Record invoice payments (`PayInvoiceAsync`), track transaction references and payment history.
  - `OrgVendorPerformance` (`/organization/vendor-performance`): View composite vendor rating scorecards, review counts, delivery reliability, and AI performance summaries.
- **Classification**: Currently configured with **complete end-to-end operational procurement execution** plus organization monitoring.

### 3.3 Outlet Manager Frontend
- **Dashboard**: `/outlet-manager` (`OutletManagerDashboard.razor`)
- **Navigation Structure**: Single comprehensive dashboard utilizing internal tabbed navigation:
  - `Dashboard Tab`: Outlet request count, pending quotations, approved requests, active contracts, recent activity log, notifications.
  - `PurchaseRequests Tab`: List purchase requests scoped strictly to `Auth.OutletID`, filter by status/date, click to view `/purchase-request/{id}`.
  - `Contracts Tab`: View active supply contracts assigned to the outlet.
  - `PurchaseOrders Tab`: View outlet POs; open PO details modal; **record delivery receipt** (`CreateDeliveryRecordAsync`, `ConfirmDeliveryRecordAsync`).
  - `Settings Tab`: Edit outlet name and street address (`UpdateOutletAsync`).
- **Main Pages & Actions**:
  - `OutletManagerDashboard`: Operational goods delivery confirmation (`ReceivedQuantity`, `SpoiledQuantity`, `DeliveryActualDate`).
  - `PurchaseRequestDetails` (`/purchase-request/{requestId}`): Read-only view of purchase request, items, associated quotation, and status.
- **Classification**: **Outlet Operational Goods Receipt & Local Outlet Monitoring**.

### 3.4 Vendor Manager Frontend
- **Dashboard**: `/vendor-dashboard` (`VendorDashboard.razor`)
- **Navigation Structure**: Portal with tabbed workspaces:
  - `Opportunities Tab`: Procurement opportunities dispatched to the vendor (`GetVendorProcurementOpportunitiesAsync`); accept/reject opportunity; navigate to submit quotation.
  - `Quotations Tab`: Submitted quotation history, validity dates, status (Submitted, Accepted, Rejected, Expired).
  - `Purchase Orders Tab`: Receive POs; accept or decline PO (`RespondToPurchaseOrderAsync`); dispatch PO (`DispatchPurchaseOrderAsync`); trigger invoice generation.
  - `Invoices Tab`: View invoices; create invoice for fulfilled PO (`CreateInvoiceAsync`); download/view invoice PDF (`DownloadInvoicePdfAsync`).
  - `Performance Tab`: View vendor rating score, quality/delivery ratings, customer feedback reviews (`GetVendorReviewsAsync`), AI performance insights.
  - `Profile Tab`: View vendor profile and assigned catalog.
- **Dedicated Subpage**:
  - `VendorProcurement` (`/vendor/procurement/{RequestId}`): Direct response portal to accept/reject opportunity and prepare/submit unit price quotation (`RespondToOpportunityAsync`, `CreateQuotationAsync`).
- **Classification**: **Vendor Operational Fulfillment & Quotation/Invoice Submission**.

---

## 4. Procurement Frontend Lifecycle Trace

```
Current Implemented Frontend Flow:
Procurement (/procurement) [Org Mgr / Outlet Mgr]
       │  (Creates PR & dispatches to selected vendors)
       ▼
Vendor Procurement (/vendor/procurement/{id} or /vendor-dashboard) [Vendor Mgr]
       │  (Accepts opportunity & submits Quotation)
       ▼
Quotations Review (/quotations, /quotation-review/{id}) [Org Mgr]
       │  (Reviews, accepts or rejects Quotation)
       ▼
Purchase Orders (/organization/purchase-orders) [Org Mgr]
       │  (Prepares & issues PO directly from accepted Quotation)
       ▼
Vendor Order Response (/vendor-dashboard) [Vendor Mgr]
       │  (Accepts PO & marks Dispatched)
       ▼
Goods Delivery (/outlet-manager) [Outlet Mgr]
       │  (Records received & spoiled qty, confirms delivery)
       ▼
Vendor Invoicing (/vendor-dashboard) [Vendor Mgr]
       │  (Generates & submits invoice with PDF)
       ▼
Invoice Review (/organization/invoices) [Org Mgr]
       │  (Performs 3-way match, Approves/Rejects invoice)
       ▼
Payment Processing (/organization/payments) [Org Mgr]
          (Submits payment method & transaction ref, marks Paid)
```

### Detailed Stage Breakdown:

| Stage | Frontend Page & Component | Primary Models & DTOs | Service Method Called | API Endpoint | Existing Role Check | Current Status Handling |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1. Purchase Request** | `/procurement`<br>`Procurement.razor` | `CreatePurchaseRequestCommand`, `CreatePurchaseRequestItemDto`, `PurchaseRequestDto` | `Api.CreatePurchaseRequestAsync()`, `Api.DispatchPurchaseRequestAsync()` | `POST api/purchaserequest`<br>`POST api/purchaserequests/{id}/dispatch` | `OrgManager`, `OutletManager`, `Admin` | Created with status `Pending` / `Submitted` |
| **2. Quotation Submission** | `/vendor/procurement/{id}`<br>`VendorProcurement.razor` | `RespondToOpportunityCommand`, `CreateQuotationCommand`, `QuotationDto` | `Api.RespondToOpportunityAsync()`, `Api.CreateQuotationAsync()` | `POST api/purchaserequests/vendor/opportunities/respond`<br>`POST api/quotation` | `VendorManager` | Opportunity: `Pending` -> `Accepted`/`Rejected`<br>Quotation: `Submitted` |
| **3. Quotation Review** | `/quotations`, `/quotation-review/{id}`<br>`QuotationsReview.razor` | `RespondToQuotationCommand`, `QuotationDto` | `Api.AcceptQuotationAsync()`, `Api.RejectQuotationAsync()`, `Api.RespondToQuotationAsync()` | `POST api/quotations/{id}/accept`<br>`POST api/quotations/{id}/reject`<br>`POST api/quotations/respond` | `OrgManager`, `Admin` | Quotation: `Submitted` -> `Accepted` or `Rejected` |
| **4. Purchase Order Creation** | `/organization/purchase-orders`<br>`OrgPurchaseOrders.razor` | `CreatePurchaseOrderCommand`, `PurchaseOrderDto` | `Api.CreatePurchaseOrderAsync()` | `POST api/purchaseorder` | `OrgManager`, `Admin` | PO: `Pending` |
| **5. PO Acceptance & Dispatch** | `/vendor-dashboard`<br>`VendorDashboard.razor` | `RespondToPurchaseOrderCommand`, `DispatchPurchaseOrderCommand` | `Api.RespondToPurchaseOrderAsync()`, `Api.DispatchPurchaseOrderAsync()` | `POST api/purchaseorder/respond`<br>`POST api/purchaseorder/dispatch` | `VendorManager` | PO: `Pending` -> `Accepted`/`Rejected` -> `Dispatched` |
| **6. Delivery Confirmation** | `/outlet-manager`<br>`OutletManagerDashboard.razor` | `CreateDeliveryRecordCommand`, `ConfirmDeliveryRecordCommand`, `DeliveryRecordDto` | `Api.CreateDeliveryRecordAsync()`, `Api.ConfirmDeliveryRecordAsync()` | `POST api/deliveries`<br>`POST api/deliveries/confirm` | `OutletManager`, `Admin` | PO: `Dispatched` -> `Delivered`<br>DeliveryRecord: `Confirmed` |
| **7. Invoice Submission** | `/vendor-dashboard`<br>`VendorDashboard.razor` | `CreateInvoiceRequest`, `InvoiceDto` | `Api.CreateInvoiceAsync()` | `POST api/invoice` | `VendorManager` | Invoice: `Pending` |
| **8. Invoice 3-Way Match & Approval** | `/organization/invoices`<br>`OrgInvoices.razor` | `RejectInvoiceRequest`, `InvoiceDto` | `Api.ApproveInvoiceAsync()`, `Api.RejectInvoiceAsync()` | `POST api/invoice/{id}/approve`<br>`POST api/invoice/{id}/reject` | `OrgManager`, `Admin` | Invoice: `Pending` -> `Approved` or `Rejected` |
| **9. Payment Recording** | `/organization/payments`<br>`OrgPayments.razor` | `MarkInvoicePaidCommand`, `PaymentDto` | `Api.PayInvoiceAsync()` | `POST api/invoice/pay` | `OrgManager`, `Admin` | Invoice: `Approved` -> `Paid`<br>Payment: `Paid` |

---

## 5. Existing Services Audit

### 5.1 Monolithic API Service (`ApiService.cs`)
`ApiService.cs` is the central service that encapsulates all REST communication with the backend.

| Domain Area | Service Method | Backend Endpoint | Models / DTOs Consumed | Consuming Pages |
| :--- | :--- | :--- | :--- | :--- |
| **Auth** | `LoginAsync` | `POST api/auth/login` | `LoginRequest`, `LoginResponseWrapper` | `Home`, `AdminLogin`, `OrgLogin`, `OutletLogin`, `VendorLogin`, `LoginForm` |
| **Auth** | `UpdateMyProfileAsync` | `PUT api/users/profile` | `UpdateMyProfileCommand`, `LoginResponse` | `AdminProfile` |
| **Organizations** | `GetOrganizationsAsync`<br>`GetOrganizationByIdAsync`<br>`CreateOrganizationAsync`<br>`UpdateOrganizationAsync` | `GET api/organizations`<br>`GET api/organizations/{id}`<br>`POST api/organizations`<br>`PUT api/organizations/{id}` | `OrganizationDto`, `CreateOrganizationCommand`, `UpdateOrganizationCommand` | `Admin`, `AdminOrganizations`, `OrgDashboard`, `Procurement`, `OrgPurchaseOrders`, `OrgInvoices`, `OrgPayments` |
| **Outlets** | `GetOutletsAsync`<br>`GetOutletsByOrgIdAsync`<br>`CreateOutletAsync`<br>`UpdateOutletAsync` | `GET api/outlets`<br>`GET api/outlets/organization/{id}`<br>`POST api/outlets`<br>`PUT api/outlets/{id}` | `OutletDto`, `CreateOutletCommand`, `UpdateOutletCommand` | `AdminOutlets`, `OrgDashboard`, `OrgOutlets`, `OutletManagerDashboard`, `Procurement`, `OrgPurchaseOrders` |
| **Products** | `GetProductsAsync`<br>`CreateProductAsync`<br>`UpdateProductAsync` | `GET api/products`<br>`POST api/products`<br>`PUT api/products/{id}` | `ProductDto`, `CreateProductCommand`, `UpdateProductCommand` | `AdminProducts`, `Procurement`, `OrgPurchaseOrders`, `OutletManagerDashboard`, `VendorDashboard` |
| **Vendors** | `GetVendorsAsync`<br>`GetVendorByIdAsync`<br>`CreateVendorAsync`<br>`UpdateVendorAsync` | `GET api/vendors`<br>`GET api/vendors/{id}`<br>`POST api/vendors`<br>`PUT api/vendors/{id}` | `VendorDto`, `CreateVendorCommand`, `UpdateVendorCommand` | `AdminVendors`, `Procurement`, `OrgDashboard`, `OrgPurchaseOrders`, `VendorDashboard` |
| **Vendor Products**| `GetVendorProductsAsync`<br>`CreateVendorProductAsync`<br>`UpdateVendorProductAsync`<br>`DeleteVendorProductAsync` | `GET api/vendorproducts`<br>`POST api/vendorproducts`<br>`PUT api/vendorproducts/{id}`<br>`DELETE api/vendorproducts/{id}` | `VendorProductDto`, `CreateVendorProductCommand`, `UpdateVendorProductCommand` | `AdminVendorProducts`, `Procurement`, `VendorDashboard` |
| **Users** | `GetUsersAsync`<br>`CreateUserAsync`<br>`UpdateUserAsync` | `GET api/users`<br>`POST api/users`<br>`PUT api/users/{id}` | `UserDto`, `CreateUserCommand`, `UpdateUserCommand` | `AdminUsers`, `Procurement`, `PurchaseRequestDetails` |
| **Tax Rates** | `GetTaxRatesAsync`<br>`CreateTaxRateAsync`<br>`UpdateTaxRateAsync` | `GET api/taxrates`<br>`POST api/taxrates`<br>`PUT api/taxrates/{id}` | `TaxRateDto`, `CreateTaxRateCommand`, `UpdateTaxRateCommand` | `AdminTaxRates`, `Admin` |
| **Purchase Requests**| `GetPurchaseRequestsAsync`<br>`GetPurchaseRequestByIdAsync`<br>`CreatePurchaseRequestAsync`<br>`DispatchPurchaseRequestAsync` | `GET api/purchaserequest`<br>`GET api/purchaserequest/{id}`<br>`POST api/purchaserequest`<br>`POST api/purchaserequests/{id}/dispatch` | `PurchaseRequestDto`, `CreatePurchaseRequestCommand`, `DispatchPurchaseRequestResponse` | `Procurement`, `PurchaseRequestDetails`, `OrgDashboard`, `OutletManagerDashboard`, `OrgPurchaseOrders` |
| **Vendor Opportunities**| `GetVendorProcurementOpportunitiesAsync`<br>`RespondToOpportunityAsync` | `GET api/purchaserequests/vendor/opportunities`<br>`POST api/purchaserequests/vendor/opportunities/respond` | `VendorProcurementOpportunityDto`, `RespondToOpportunityCommand` | `VendorDashboard`, `VendorProcurement` |
| **Vendor Recommendations**| `GetVendorRecommendationsAsync` | `GET api/purchaserequests/{id}/recommendations` | `VendorRecommendationResponse`, `VendorRecommendationDto` | `Procurement` |
| **Quotations** | `GetQuotationsAsync`<br>`GetQuotationByIdAsync`<br>`CreateQuotationAsync`<br>`AcceptQuotationAsync`<br>`RejectQuotationAsync`<br>`RespondToQuotationAsync` | `GET api/quotation`<br>`GET api/quotation/{id}`<br>`POST api/quotation`<br>`POST api/quotations/{id}/accept`<br>`POST api/quotations/{id}/reject`<br>`POST api/quotations/respond` | `QuotationDto`, `CreateQuotationCommand`, `RespondToQuotationCommand` | `QuotationsReview`, `QuotationReview`, `VendorProcurement`, `OrgDashboard`, `OrgPurchaseOrders` |
| **Contracts** | `GetContractsAsync`<br>`GetContractsByOutletAsync`<br>`GetContractsByOrganizationAsync`<br>`GetContractByIdAsync`<br>`CreateContractAsync`<br>`CreateContractFromQuotationAsync`<br>`ResetContractAsync` | `GET api/contract`<br>`GET api/contract/outlet/{id}`<br>`GET api/contract/organization/{id}`<br>`GET api/contract/{id}`<br>`POST api/contract`<br>`POST api/contract/from-quotation`<br>`POST api/contract/reset` | `ContractDto`, `CreateContractCommand`, `CreateContractFromQuotationCommand`, `ResetContractCommand` | `AdminContracts`, `OrgContracts`, `CreateContract`, `ContractDetails`, `OrgDashboard`, `OutletManagerDashboard` |
| **Purchase Orders** | `GetPurchaseOrdersAsync`<br>`GetPurchaseOrderByIdAsync`<br>`GetPendingPurchaseOrdersByVendorAsync`<br>`CreatePurchaseOrderAsync`<br>`RespondToPurchaseOrderAsync`<br>`DispatchPurchaseOrderAsync` | `GET api/purchaseorder`<br>`GET api/purchaseorder/{id}`<br>`GET api/purchaseorder/vendor/{id}/pending`<br>`POST api/purchaseorder`<br>`POST api/purchaseorder/respond`<br>`POST api/purchaseorder/dispatch` | `PurchaseOrderDto`, `CreatePurchaseOrderCommand`, `RespondToPurchaseOrderCommand`, `DispatchPurchaseOrderCommand` | `OrgPurchaseOrders`, `OutletManagerDashboard`, `VendorDashboard`, `OrgInvoices` |
| **Deliveries** | `GetDeliveryRecordsByPurchaseOrderAsync`<br>`CreateDeliveryRecordAsync`<br>`ConfirmDeliveryRecordAsync` | `GET api/deliveries/po/{poId}`<br>`POST api/deliveries`<br>`POST api/deliveries/confirm` | `DeliveryRecordDto`, `CreateDeliveryRecordCommand`, `ConfirmDeliveryRecordCommand` | `OutletManagerDashboard`, `OrgPurchaseOrders`, `OrgInvoices` |
| **Invoices** | `GetInvoicesAsync`<br>`GetInvoiceByIdAsync`<br>`CreateInvoiceAsync`<br>`ApproveInvoiceAsync`<br>`RejectInvoiceAsync`<br>`DownloadInvoicePdfAsync` | `GET api/invoice`<br>`GET api/invoice/{id}`<br>`POST api/invoice`<br>`POST api/invoice/{id}/approve`<br>`POST api/invoice/{id}/reject`<br>`GET api/invoice/{id}/download` | `InvoiceDto`, `CreateInvoiceRequest`, `RejectInvoiceRequest` | `OrgInvoices`, `VendorDashboard`, `OrgPayments` |
| **Payments** | `GetPaymentsAsync`<br>`PayInvoiceAsync` | `GET api/invoice/payments`<br>`POST api/invoice/pay` | `PaymentDto`, `MarkInvoicePaidCommand` | `OrgPayments` |
| **Notifications**| `GetMyNotificationsAsync`<br>`MarkNotificationReadAsync` | `GET api/notifications`<br>`PUT api/notifications/{id}/read` | `NotificationDto` | `OrgDashboard`, `OutletManagerDashboard`, `VendorDashboard`, `OrgPurchaseOrders`, `PurchaseRequestDetails` |
| **Vendor Performance & AI**| `GetVendorPerformanceSummariesAsync`<br>`GetVendorReviewsAsync`<br>`GetEligibleReviewOrdersAsync`<br>`CreateVendorReviewAsync`<br>`GetVendorAiInsightsAsync` | `GET api/VendorPerformance`<br>`GET api/VendorFeedback/vendor/{id}`<br>`GET api/VendorFeedback/eligible-orders`<br>`POST api/VendorFeedback`<br>`GET api/vendorfeedback/vendor/{id}/ai-insights` | `VendorPerformanceSummaryDto`, `VendorReviewDto`, `EligibleReviewOrderDto`, `CreateVendorReviewRequest`, `VendorAiInsightsDto` | `OrgVendorPerformance`, `Procurement`, `OrgPurchaseOrders`, `VendorDashboard`, `SubmitReviewModal` |

### 5.2 Micro-Services in `Services/` Folders
In addition to `ApiService.cs`, specialized domain services exist across `Services/PurchaseRequests/`, `Services/Quotations/`, `Services/PurchaseOrders/`, `Services/Deliveries/`, `Services/Invoices/`, etc. These wrap `Services.Infrastructure.ApiClient`. However, `Program.cs` registers `ApiService` directly as the unified typed client used by all Razor pages.

---

## 6. Existing Models & DTOs

| Category | Model Name | Location | Description & Usage |
| :--- | :--- | :--- | :--- |
| **Auth** | `LoginRequest`<br>`LoginResponse`<br>`LoginResponseWrapper` | `Models/Authentication/` | User credentials payload and authenticated token/claims session model. |
| **Purchase Requests** | `PurchaseRequestDto`<br>`PurchaseRequestItemDto`<br>`CreatePurchaseRequestCommand`<br>`CreatePurchaseRequestItemDto` | `Models/PurchaseRequests/` | Represents purchase request headers and line items (Product, Quantity, Unit, Status, Outlet). |
| **Vendor Opportunities** | `VendorProcurementOpportunityDto`<br>`RespondToOpportunityCommand` | `Models/Procurement/`<br>`Models/PurchaseRequests/` | Vendor-side view of incoming dispatched requests with accept/reject action. |
| **Recommendations** | `VendorRecommendationDto`<br>`VendorRecommendationResponse` | `Models/VendorRecommendations/` | AI-weighted multi-factor vendor scorecards (Composite Score, Quality, Delivery, Contract status). |
| **Quotations** | `QuotationDto`<br>`QuotationItemDto`<br>`CreateQuotationCommand`<br>`RespondToQuotationCommand` | `Models/Quotations/` | Vendor price quotation line items (UnitPrice, TaxRate, TaxAmount, ValidUntil, Status). |
| **Contracts** | `ContractDto`<br>`CreateContractCommand`<br>`CreateContractFromQuotationCommand`<br>`CreateContractVendorAllocationDto` | `Models/Contracts/` | Long-term supply agreements and multi-vendor split percentage volume allocations. |
| **Purchase Orders** | `PurchaseOrderDto`<br>`PurchaseOrderItemDto`<br>`CreatePurchaseOrderCommand`<br>`RespondToPurchaseOrderCommand`<br>`DispatchPurchaseOrderCommand` | `Models/PurchaseOrders/` | Official Purchase Order binding Quotation to Vendor and Outlet with expected/actual delivery timestamps. |
| **Deliveries** | `DeliveryRecordDto`<br>`CreateDeliveryRecordCommand`<br>`ConfirmDeliveryRecordCommand` | `Models/Deliveries/` | Goods receipt record with received vs spoiled quantities, confirming user ID, and timestamps. |
| **Invoices** | `InvoiceDto`<br>`InvoiceItemDto`<br>`CreateInvoiceRequest`<br>`RejectInvoiceRequest` | `Models/Invoices/` | Vendor billing records containing line items, PDF base64 payloads, and 3-way match metadata. |
| **Payments** | `PaymentDto`<br>`MarkInvoicePaidCommand` | `Models/Payments/` | Invoice payment settlement record (PaymentMethod, TransactionReference, PaymentDate, Amount). |
| **Performance & AI** | `VendorPerformanceSummaryDto`<br>`VendorReviewDto`<br>`EligibleReviewOrderDto`<br>`CreateVendorReviewRequest`<br>`VendorAiInsightsDto` | `Models/VendorPerformance/` | 5-star quality/delivery ratings, text reviews, Gemini AI vendor performance summaries. |
| **Notifications** | `NotificationDto`<br>`GetNotificationsResponse` | `Models/Notifications/` | User-scoped in-app alerts with unread flags, navigation links, and notification types. |

---

## 7. Existing UI & Design System

### 7.1 Visual Palette & Design Tokens
The design system defined in `wwwroot/app.css` establishes a modern enterprise palette:

- **Primary Accent (Soft Sage)**: `--app-primary: #5F806B`, hover `#4F705C`, soft `#F4F8F5`.
- **Secondary Accent (Subtle Blush)**: `--app-secondary: #C9828B`, hover `#B96F78`, light `#F8ECEE`.
- **Canvas & Backgrounds**: `--app-bg: #F7F9F7`, secondary `#F1F5F2`, surface `#FFFFFF`.
- **Typography & Neutrals**: `--app-text: #26312B`, secondary `#66736B`, muted `#8A948E`.
- **Borders & Dividers**: `--app-border: #E1E8E3`, hover `#CBD8CF`.
- **Status Accents**:
  - Success (Green): `#4F8062` / bg `#EAF4ED`
  - Warning (Amber): `#B48745` / bg `#FBF4E7`
  - Danger (Rose/Red): `#B85C63` / bg `#FBECEE`
  - Info (Slate Blue): `#5B7890` / bg `#EDF3F7`
- **Typography**: `Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto...` (clean sans-serif).
- **Geometry & Border Radii**: Strict professional rectangular design tokens (`--app-radius-sm: 2px`, `--app-radius: 4px`, `--app-radius-xl: 6px`).
- **Global Ambient Orbs**: Fixed subtle pseudo-elements (`body::before` sage orb top-right, `body::after` blush orb bottom-left).

### 7.2 Core Reusable UI Patterns & Components
1. **Layout Shell**: Fixed sidebar (`.admin-sidebar`, `.org-sidebar`) with collapsible toggle (`.sidebar-collapsed`), brand emblem, user profile menu, and live notifications popover.
2. **Metric KPI Cards**: Multi-metric stat cards (`.stat-card`, `.svm-metric-card`) with numeric metrics, trend badges, and SVG icons.
3. **Data Tables**: Standardized responsive tables (`.app-table`, `.svm-table`) with sortable headers, hover states, empty state placeholders, and formatted currency cells.
4. **Status Badges**: Standardized pill badges (`.badge-approved`, `.badge-pending`, `.badge-rejected`, `.po-status-dispatched`, `.po-status-delivered`).
5. **Modals & Overlays**: Clean backdrop modals (`.svm-modal-backdrop`, `.app-modal`) for forms, confirmations, and review submissions.
6. **Form Controls**: Unified styled inputs (`.form-control`, `.svm-input`), focus rings, validation text alerts.
7. **Document Interop**: Dedicated action buttons for viewing PDF binaries inline and downloading invoices with base64-to-blob conversion.

---

## 8. Current Procurement Responsibilities (As Implemented)

Based **strictly on what the current codebase implements**, the operational responsibilities are distributed as follows:

| Procurement Action | Admin | Organization Manager | Outlet Manager | Vendor Manager |
| :--- | :---: | :---: | :---: | :---: |
| **Create Purchase Request** | Yes (via `/procurement`) | **Yes** (Primary in `/procurement`) | Yes (via `/procurement` tab) | No |
| **View Purchase Requests** | Yes (Global) | **Yes** (Organization scoped) | **Yes** (Outlet scoped) | Yes (Dispatched opportunities only) |
| **Accept/Reject Dispatched PR** | No | No | No | **Yes** (via `/vendor-dashboard` or `/vendor/procurement/{id}`) |
| **Prepare & Submit Quotation** | No | No | No | **Yes** (via `/vendor/procurement/{id}`) |
| **View & Compare Quotations** | Yes (Global) | **Yes** (via `/quotations`) | No | Yes (Own quotations only) |
| **Accept / Reject Quotation** | Yes | **Yes** (via `/quotation-review/{id}`) | No | No |
| **Create Long-term Contract** | Yes (Global) | **Yes** (via `/contract/create/{id}`) | No | No |
| **Prepare & Issue Purchase Order** | Yes | **Yes** (via `/organization/purchase-orders`) | No | No |
| **Approve / Reject Purchase Order** | No (Not implemented as separate action) | **No** (Directly creates PO) | No | No |
| **Accept / Decline PO** | No | No | No | **Yes** (via `/vendor-dashboard`) |
| **Dispatch / Fulfill PO** | No | No | No | **Yes** (via `/vendor-dashboard`) |
| **Record & Confirm Delivery** | Yes | No | **Yes** (via `/outlet-manager` PO modal) | No |
| **Submit Goods Feedback / Review**| Yes | **Yes** (via `/organization/purchase-orders`) | No | No |
| **Create & Submit Invoice** | No | No | No | **Yes** (via `/vendor-dashboard`) |
| **Approve / Reject Invoice** | Yes | **Yes** (via `/organization/invoices`) | No | No |
| **Process / Record Payment** | Yes | **Yes** (via `/organization/payments`) | No | No |

---

## 9. Frontend Gaps for the Future Workflow

### 9.1 Target Workflow & Business Rules Summary
- **Admin**: Sets up Organizations, Outlets, Users, Vendors, Products, VendorProducts, TaxRates. (Does not act as normal PO approver).
- **Purchase Manager (New Role)**:
  - Creates Purchase Request.
  - Purchase Request goes **directly** to relevant Vendor Manager(s) without intermediate approval.
  - Compares received quotations and accepts/selects the winning quotation.
  - Prepares Purchase Order from accepted quotation.
  - Submits PO to configured approver.
  - Once PO is approved, sends/finalizes approved PO to Vendor Manager.
  - Records delivery receipt and provides goods-received feedback/ratings.
  - Handles operational invoice verification and payment processing.
- **Organization Manager**:
  - Acts as the **configured PO Approver** (Approves / Rejects PO).
  - Retains organization-level monitoring and audit visibility across PRs, Quotations, POs, Deliveries, Invoices, Payments, Contracts, and Vendor Performance.
- **Outlet Manager**:
  - Primarily monitoring outlet operations (may support configured PO approval architecture in future without hardcoding extra approval stages now).
- **Vendor Manager**:
  - Receives PRs directly -> provides Quotations -> receives approved PO -> dispatches goods -> submits Invoice -> receives payment.

### 9.2 Comprehensive Gap Analysis Matrix

| # | Workflow Phase / Feature | Current State in Frontend | Required Future State | Reusable Existing Artifacts | Required Modifications / Additions | Scope Type |
| :- | :--- | :--- | :--- | :--- | :--- | :--- |
| **1** | **Purchase Manager Role & Auth** | No Purchase Manager role exists in `AuthService` or login routing. | Add `Purchase Manager` role support, `Auth.IsPurchaseManager`, login redirect to `/purchase-manager/dashboard` (or dedicated workspace), and route protection. | `AuthService.cs`, `LoginForm.razor`, `Home.razor` | Update `AuthService.cs` (`IsPurchaseManager`), update `Home.razor.cs` and `LoginForm.razor.cs` routing. | Frontend (Matches backend `Purchase Manager` role) |
| **2** | **Purchase Request Creation** | Shared between Org Manager and Outlet Manager in `/procurement`. | Purchase Manager creates PR; PR is dispatched directly to Vendor Managers. PR approval does NOT exist. | `Procurement.razor`, `Procurement.razor.cs`, `CreatePurchaseRequestCommand` | Repurpose or create Purchase Manager PR interface reusing `Procurement.razor`'s 3-step recommendation matrix. | Frontend |
| **3** | **Quotation Selection & Acceptance** | Org Manager reviews and accepts quotations in `/quotations`. | Purchase Manager reviews, compares, and accepts/selects quotations. | `QuotationsReview.razor`, `QuotationReview.razor`, `QuotationDto` | Provide Purchase Manager access to quotation review/selection; Org Manager retains read-only audit/monitoring view. | Frontend |
| **4** | **PO Preparation vs Approval** | Org Manager creates PO in `/organization/purchase-orders` which directly becomes `Pending` for Vendor without explicit internal approval. | Purchase Manager **prepares** PO; PO goes to configured approver (**Organization Manager**); Org Manager **Approves / Rejects** PO; Purchase Manager finalizes/sends approved PO to Vendor. | `OrgPurchaseOrders.razor`, `PurchaseOrderDto`, `CreatePurchaseOrderCommand` | 1. Purchase Manager prepares PO.<br>2. Add Org Manager **PO Approval / Rejection UI** (`Approve PO` / `Reject PO` buttons calling backend PO approval endpoint).<br>3. PO status transitions updated to reflect `Pending Approval` -> `Approved` / `Rejected` -> `Sent to Vendor`. | Frontend + Backend PO Approval API endpoints |
| **5** | **Delivery Recording & Feedback** | Delivery is currently recorded in `OutletManagerDashboard.razor`, feedback is on `OrgPurchaseOrders.razor`. | Purchase Manager records delivery receipt and submits goods-received feedback/ratings. | `OutletManagerDashboard` delivery modal logic, `SubmitReviewModal.razor`, `DeliveryRecordDto` | Integrate delivery recording and goods review modal into the Purchase Manager operational workspace; Outlet Manager retains local monitoring. | Frontend |
| **6** | **Invoice Verification & Payment Processing** | Org Manager approves invoices (`OrgInvoices`) and records payments (`OrgPayments`). | Purchase Manager handles operational 3-way invoice matching and payment recording. Org Manager retains organization-level financial audit visibility. | `OrgInvoices.razor`, `OrgPayments.razor`, `InvoiceDto`, `PaymentDto` | Move or grant operational invoice approval/payment actions to Purchase Manager; configure Org Manager views for oversight and auditing. | Frontend |
| **7** | **Organization Manager Oversight** | Org Manager performs all operational actions directly. | Org Manager focuses on **PO Approval** and organization-wide monitoring across Outlets, Contracts, POs, Invoices, and Vendor Ratings. | `OrgDashboard.razor`, `OrgContracts.razor`, `OrgInvoices.razor`, `OrgPayments.razor` | Add dedicated PO Approval workspace/tab to Org Manager dashboard; ensure read-only audit access across procurement pipeline. | Frontend |

---

## 10. Preservation & Risk Analysis

### 10.1 High-Value Functionality That Must NOT Be Broken
1. **Vendor Portal Lifecycle**:
   - The entire vendor workflow in `VendorDashboard.razor` and `VendorProcurement.razor` (opportunity acceptance, quotation submission, PO response, dispatching, invoice creation, PDF generation) is fully developed and must remain intact.
2. **Admin Master Data Infrastructure**:
   - All 10 Admin management pages (Organizations, Outlets, Products, Vendors, VendorProducts, TaxRates, Users, Profile) operate cleanly and must remain untouched.
3. **AI Vendor Recommendation Matrix**:
   - The multi-factor scoring calculation (Quality, Delivery, Price, Contracts, Spoilage) in `Procurement.razor.cs` and `VendorPerformance` modals is mature and must be preserved.
4. **Contract Allocation System**:
   - Multi-vendor split-percentage contracts in `CreateContract` and `OrgContracts` must be preserved.
5. **PDF Interop & Document Downloading**:
   - `window.downloadFileFromBase64` and `window.viewPdfFromBase64` in `App.razor` must continue functioning for invoices and purchase orders.

### 10.2 Role Logic & Regression Risks
- **Shared Routes**: If routes like `/procurement` or `/quotations` are modified, ensure role guard checks do not inadvertently lock out authorized managers or crash on null organization IDs.
- **Session Scoping**: All queries rely on `Auth.OrganizationID`, `Auth.OutletID`, or `Auth.VendorID`. The new Purchase Manager role must properly carry `OrganizationID` (and optional default `OutletID`) to maintain data scoping.
- **CSS Isolation**: The scoped CSS files contain complex layout rules. Refactoring must reuse existing CSS classes rather than introducing conflicting global styles.

---

## 11. Recommended Implementation Order (Future Phases)

> [!NOTE]
> This is a planned roadmap for subsequent implementation. **No changes have been executed during this audit.**

### Phase 1: Authentication & Role Infrastructure
- Extend `AuthService.cs` with `IsPurchaseManager` (`Role == "Purchase Manager"`).
- Update `Home.razor` and `LoginForm.razor` to recognize the `Purchase Manager` role and route to the Purchase Manager workspace.
- **Checkpoint**: Test login with credentials for all 5 roles (Admin, Org Manager, Purchase Manager, Outlet Manager, Vendor Manager) and verify correct dashboard routing.

### Phase 2: Purchase Manager Operational Workspace & PR Creation
- Establish the Purchase Manager dashboard and navigation.
- Wire PR creation and direct dispatch to Vendor Managers (reusing the existing recommendation matrix).
- Ensure PRs skip any internal approval and appear directly in Vendor Manager opportunities.
- **Checkpoint**: Create PR as Purchase Manager -> verify immediate appearance in Vendor Manager opportunity inbox.

### Phase 3: Quotation Review & Selection by Purchase Manager
- Provide quotation comparison and acceptance interface for Purchase Manager.
- Ensure accepting a quotation allows the Purchase Manager to prepare a Purchase Order.
- **Checkpoint**: Vendor submits Quotation -> Purchase Manager reviews and accepts Quotation.

### Phase 4: Purchase Order Preparation & Organization Manager Approval
- Implement Purchase Manager PO preparation from accepted quotation.
- Implement Organization Manager PO Approval / Rejection UI on `OrgPurchaseOrders.razor` or Org Dashboard.
- Once approved by Org Manager, allow Purchase Manager to issue/send PO to Vendor Manager.
- **Checkpoint**: Purchase Manager prepares PO -> Org Manager approves PO -> Vendor Manager receives and dispatches PO.

### Phase 5: Goods Receipt, Delivery Confirmation & Reviews
- Enable Purchase Manager to record delivered goods and confirm receipt.
- Enable Purchase Manager to submit goods review and 5-star ratings via `SubmitReviewModal`.
- **Checkpoint**: Vendor dispatches -> Purchase Manager records delivery -> PO status updates to `Delivered`.

### Phase 6: Operational Invoice Matching & Payment Processing
- Enable Purchase Manager to perform 3-way invoice matching and approve verified invoices.
- Enable Purchase Manager to record invoice payments with transaction references.
- Retain Org Manager audit view for financial oversight.
- **Checkpoint**: Vendor creates Invoice -> Purchase Manager approves invoice and executes payment -> Invoice marked `Paid`.

---

## 12. Final File Impact Summary

| File / Folder | Current Purpose | Future Action | Reason |
| :--- | :--- | :---: | :--- |
| `Services/AuthService.cs` | Authentication and session state manager | **Modify** | Add `IsPurchaseManager` role check and helper properties. |
| `Components/Pages/Home/Home.razor(.cs)` | Universal login and dashboard router | **Modify** | Add post-login redirect for Purchase Manager. |
| `Components/Shared/LoginForm/LoginForm.razor(.cs)` | Parametric login component | **Modify** | Add role validation support for Purchase Manager. |
| `Services/ApiService.cs` | Central typed HTTP API client | **Modify** | Add endpoints for PO approval/rejection and any Purchase Manager-specific APIs. |
| `Components/Pages/Procurement/` | Purchase request creation & recommendation dispatch | **Keep / Reuse** | Reuse core 3-step recommendation UI for Purchase Manager. |
| `Components/Pages/QuotationsReview/` | Quotations list & review | **Keep / Modify** | Enable Purchase Manager selection; preserve Org Manager audit view. |
| `Components/Pages/QuotationReview/` | Single quotation review & acceptance | **Keep / Modify** | Purchase Manager executes acceptance; transition to PO preparation. |
| `Components/Pages/OrgPurchaseOrders/` | PO management & creation | **Modify** | Add Org Manager PO Approval / Rejection action buttons; support PM preparation. |
| `Components/Pages/OrgDashboard/` | Organization Manager dashboard | **Modify** | Highlight pending PO Approvals and organization oversight metrics. |
| `Components/Pages/OrgInvoices/` | Invoice review & 3-way match | **Keep / Modify** | Retain Org Manager audit visibility; share matching UI with PM. |
| `Components/Pages/OrgPayments/` | Invoice payment settlement | **Keep / Modify** | Retain Org Manager audit visibility; share payment execution with PM. |
| `Components/Pages/OutletManagerDashboard/` | Outlet dashboard & goods receipt | **Keep** | Retain outlet monitoring; delivery receipt available to PM. |
| `Components/Pages/VendorDashboard/` | Complete Vendor fulfillment workspace | **Keep** | Preserve existing vendor lifecycle (opportunities, quotations, orders, invoices). |
| `Components/Pages/VendorProcurement/` | Vendor quotation preparation | **Keep** | Preserved without breaking changes. |
| `Components/Pages/Admin/` (and subpages) | Admin dashboards and CRUD masters | **Keep** | Preserved completely unchanged. |
| `Components/Pages/CreateContract/` | Contract creation & allocation | **Keep** | Preserved for contract workflows. |
| `Components/Shared/SubmitReviewModal/` | Vendor feedback submission modal | **Keep / Reuse** | Consumed by Purchase Manager after delivery. |
| `wwwroot/app.css` | Global Design System & tokens | **Keep** | Visual baseline preserved with zero style regressions. |

---

## Explicit Audit Confirmation
- **No source files were modified.**
- **No files were created or deleted (except saving this audit report).**
- **No configuration was changed.**
- **No database changes or migrations were executed.**
