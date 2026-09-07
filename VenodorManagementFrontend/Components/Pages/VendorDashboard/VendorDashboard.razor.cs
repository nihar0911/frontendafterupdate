using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Models.Invoices.DTOs;
using VenodorManagementFrontend.Models.Invoices.Requests;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.VendorDashboard;

public partial class VendorDashboard : ComponentBase
{
    private List<VendorProcurementOpportunityDto>? Opportunities { get; set; }
    private List<QuotationDto> AllQuotations { get; set; } = new();
    private List<ContractDto> ActiveContracts { get; set; } = new();
    private List<PurchaseRequestDto> AllPurchaseRequests { get; set; } = new();
    private Dictionary<int, PurchaseRequestDto> RequestCache { get; set; } = new();
    private Dictionary<int, string> ProductDictionary { get; set; } = new();
    private Dictionary<int, string> OutletDictionary { get; set; } = new();
    private Dictionary<int, string> OrgDictionary { get; set; } = new();
    private Dictionary<int, int> OutletOrgMap { get; set; } = new();

    private string VendorName { get; set; } = string.Empty;

    private List<NotificationDto> Notifications { get; set; } = new();
    private int UnreadNotificationCount => Notifications.Count(n => !n.IsRead);

    private string ActiveSidebarNav { get; set; } = "Dashboard";
    private List<VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto> MyReviews = new();
    private bool ShowReviewDetailsModal { get; set; } = false;
    private VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto? SelectedReviewDetails { get; set; }
    private string? ReviewLoadErrorMessage { get; set; }
    private VenodorManagementFrontend.Models.VendorPerformance.VendorPerformanceSummaryDto? MyPerformance;
    private string ActiveTab { get; set; } = "new";
    private string QuotationStatusFilter { get; set; } = "All";
    private string ContractStatusFilter { get; set; } = "Active";
    private string SearchQuery { get; set; } = string.Empty;
    private bool IsLoading { get; set; } = true;
    private bool ShowNotificationDropdown { get; set; } = false;

    // Selected Quotation for dedicated Details View
    private QuotationDto? SelectedQuotation { get; set; }
    private PurchaseRequestDto? SelectedQuotationPR { get; set; }

    // Selected Contract for dedicated Details View
    private ContractDto? SelectedContract { get; set; }

    // Purchase Orders state for Vendor Manager
    private List<PurchaseOrderDto> MyPurchaseOrders { get; set; } = new();
    private string PoStatusFilter { get; set; } = "All";
    private int PendingPoCount => MyPurchaseOrders.Count(p => string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase));

    // Dispatch Modal & AI Spoilage Advisor state
    private PurchaseOrderDto? SelectedPoForDispatch { get; set; }
    private bool ShowDispatchModal { get; set; } = false;
    private bool IsLoadingSpoilageAdvice { get; set; } = false;
    private SpoilageAdvisorDto? SpoilageAdvisorData { get; set; }
    private string? SpoilageAdviceError { get; set; }
    private bool IsDispatchingPo { get; set; } = false;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private List<InvoiceDto> MyInvoices { get; set; } = new();
    private string InvoiceSubTab { get; set; } = "Ready";
    private PurchaseOrderDto? SelectedPoForInvoice { get; set; }
    private List<DeliveryRecordDto> SelectedPoDeliveryRecords { get; set; } = new();
    private bool IsSubmittingInvoice { get; set; } = false;
    private string InvoiceSubmitError { get; set; } = string.Empty;
    private string InvoiceSubmitSuccess { get; set; } = string.Empty;
    private bool ShowCreateInvoiceModal { get; set; } = false;
    private bool IsLoadingDeliveryData { get; set; } = false;

    private List<PurchaseOrderDto> EligiblePurchaseOrders =>
        MyPurchaseOrders
            .Where(po => string.Equals(po.Status, "Delivered", StringComparison.OrdinalIgnoreCase) &&
                         !MyInvoices.Any(inv => inv.PurchaseOrderID == po.PurchaseOrderID))
            .ToList();

    private List<QuotationDto> PendingQuotations => AllQuotations
        .Where(q => string.Equals(q.Status, "Submitted", StringComparison.OrdinalIgnoreCase) || string.Equals(q.Status, "Pending", StringComparison.OrdinalIgnoreCase))
        .ToList();

    private List<QuotationDto> AcceptedQuotations => AllQuotations
        .Where(q => string.Equals(q.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
        .ToList();

    private List<QuotationDto> RejectedQuotations => AllQuotations
        .Where(q => string.Equals(q.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
        .ToList();

    private int ActiveContractCount => ActiveContracts.Count(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(c.Status));
    private int ExpiredContractCount => ActiveContracts.Count(c => string.Equals(c.Status, "Expired", StringComparison.OrdinalIgnoreCase) || string.Equals(c.Status, "Completed", StringComparison.OrdinalIgnoreCase) || string.Equals(c.Status, "Terminated", StringComparison.OrdinalIgnoreCase));

    private VendorProcurementOpportunityDto? TopOpportunity => Opportunities?.FirstOrDefault(o => o.OpportunityStatus == "Pending" || o.OpportunityStatus == "Accepted");

    private IEnumerable<VendorProcurementOpportunityDto> FilteredOpportunities
    {
        get
        {
            if (Opportunities == null) return Enumerable.Empty<VendorProcurementOpportunityDto>();
            var list = Opportunities.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim().ToLowerInvariant();
                list = list.Where(o =>
                    $"PR-{o.RequestID}".ToLowerInvariant().Contains(q) ||
                    o.RequestID.ToString().Contains(q) ||
                    (!string.IsNullOrEmpty(o.ProductName) && o.ProductName.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(o.OutletName) && o.OutletName.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(o.OrganizationName) && o.OrganizationName.ToLowerInvariant().Contains(q))
                );
            }

            return list;
        }
    }

    private IEnumerable<QuotationDto> FilteredQuotationsList
    {
        get
        {
            var list = AllQuotations.AsEnumerable();

            if (QuotationStatusFilter == "Pending")
            {
                list = list.Where(q => string.Equals(q.Status, "Submitted", StringComparison.OrdinalIgnoreCase) || string.Equals(q.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            }
            else if (QuotationStatusFilter == "Accepted")
            {
                list = list.Where(q => string.Equals(q.Status, "Accepted", StringComparison.OrdinalIgnoreCase));
            }
            else if (QuotationStatusFilter == "Rejected")
            {
                list = list.Where(q => string.Equals(q.Status, "Rejected", StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim().ToLowerInvariant();
                list = list.Where(item =>
                    $"QT-{item.QuotationID}".ToLowerInvariant().Contains(q) ||
                    item.QuotationID.ToString().Contains(q) ||
                    $"PR-{item.RequestID}".ToLowerInvariant().Contains(q) ||
                    item.RequestID.ToString().Contains(q) ||
                    (!string.IsNullOrEmpty(item.Status) && item.Status.ToLowerInvariant().Contains(q)) ||
                    GetProductName(item).ToLowerInvariant().Contains(q) ||
                    GetOutletName(item).ToLowerInvariant().Contains(q) ||
                    GetOrgName(item).ToLowerInvariant().Contains(q)
                );
            }

            return list;
        }
    }

    private IEnumerable<ContractDto> FilteredContractsList
    {
        get
        {
            var list = ActiveContracts.AsEnumerable();

            if (ContractStatusFilter == "Active")
            {
                list = list.Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(c.Status));
            }
            else if (ContractStatusFilter == "Expired")
            {
                list = list.Where(c => string.Equals(c.Status, "Expired", StringComparison.OrdinalIgnoreCase) || string.Equals(c.Status, "Completed", StringComparison.OrdinalIgnoreCase) || string.Equals(c.Status, "Terminated", StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim().ToLowerInvariant();
                list = list.Where(c =>
                    $"Contract {c.ContractID}".ToLowerInvariant().Contains(q) ||
                    c.ContractID.ToString().Contains(q) ||
                    GetContractOutletName(c).ToLowerInvariant().Contains(q) ||
                    GetContractOrgName(c).ToLowerInvariant().Contains(q) ||
                    GetContractProductName(c).ToLowerInvariant().Contains(q) ||
                    (!string.IsNullOrEmpty(c.Status) && c.Status.ToLowerInvariant().Contains(q))
                );
            }

            return list;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await InitializeDashboard();
    }

    protected override async Task OnParametersSetAsync()
    {
        await InitializeDashboard();
    }

    private async Task InitializeDashboard()
    {
        if (Auth.IsAuthenticated && (Auth.IsVendorManager || Auth.IsAdmin))
        {
            if (!Auth.VendorID.HasValue || Auth.VendorID.Value <= 0)
            {
                try
                {
                    var users = await Api.GetUsersAsync();
                    var me = users?.FirstOrDefault(u => u.UserID == Auth.UserID || string.Equals(u.Email, Auth.CurrentUser?.Email, StringComparison.OrdinalIgnoreCase));
                    if (me != null && me.VendorID.HasValue && me.VendorID.Value > 0)
                    {
                        if (Auth.CurrentUser != null)
                        {
                            Auth.CurrentUser.VendorID = me.VendorID.Value;
                        }
                    }
                }
                catch { }
            }

            if (Auth.VendorID.HasValue && Auth.VendorID.Value > 0)
            {
                await LoadDashboardData();
            }
            else
            {
                IsLoading = false;
                StateHasChanged();
            }
        }
        else
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadDashboardData()
    {
        IsLoading = true;
        StateHasChanged();

        try
        {
            // 1. Resolve Vendor Name
            if (Auth.VendorID.HasValue)
            {
                var vendors = await Api.GetVendorsAsync();
                var v = vendors?.FirstOrDefault(ven => ven.VendorID == Auth.VendorID.Value);
                VendorName = v?.VendorName ?? $"Vendor #{Auth.VendorID.Value}";
                try
                {
                    MyPerformance = await Api.GetVendorPerformanceByIdAsync(Auth.VendorID.Value);
                    MyReviews = await Api.GetVendorReviewsAsync(Auth.VendorID.Value);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[VendorDashboard] Error loading performance/reviews: {ex.Message}");
                }
            }

            // 2. Load Real Opportunities
            Opportunities = await Api.GetVendorProcurementOpportunitiesAsync() ?? new List<VendorProcurementOpportunityDto>();
            if (Opportunities != null)
            {
                foreach (var opp in Opportunities)
                {
                    if (opp.ProductID > 0 && !string.IsNullOrWhiteSpace(opp.ProductName) && !opp.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
                    {
                        ProductDictionary[opp.ProductID] = opp.ProductName;
                    }
                }
            }

            // 3. Load Quotations for this vendor
            var allQ = await Api.GetQuotationsAsync();
            if (allQ != null && Auth.VendorID.HasValue)
            {
                AllQuotations = allQ.Where(q => q.VendorID == Auth.VendorID.Value).ToList();
            }

            // 4. Load Products from main catalog
            try
            {
                var products = await Api.GetProductsAsync();
                if (products != null)
                {
                    foreach (var p in products)
                    {
                        if (!string.IsNullOrWhiteSpace(p.ProductName))
                        {
                            ProductDictionary[p.ProductID] = p.ProductName;
                        }
                    }
                }
            }
            catch { }

            // 5. Load Outlets & Organizations
            try
            {
                var outlets = await Api.GetOutletsAsync();
                if (outlets != null)
                {
                    foreach (var o in outlets)
                    {
                        var name = !string.IsNullOrWhiteSpace(o.OutletName)
                            ? o.OutletName
                            : (!string.IsNullOrWhiteSpace(o.Address) ? $"{o.Address.Split(',')[0].Trim()} Outlet" : $"Outlet #{o.OutletID}");
                        OutletDictionary[o.OutletID] = name;
                        OutletOrgMap[o.OutletID] = o.OrganizationID;
                    }
                }

                var orgs = await Api.GetOrganizationsAsync();
                if (orgs != null)
                {
                    foreach (var org in orgs)
                    {
                        OrgDictionary[org.OrganizationID] = org.OrganizationName;
                    }
                }
            }
            catch { }

            // 6. Load Contracts for this vendor
            var allContracts = await Api.GetContractsAsync();
            if (allContracts != null && Auth.VendorID.HasValue)
            {
                ActiveContracts = allContracts.Where(c =>
                    c.VendorID == Auth.VendorID.Value ||
                    (c.Allocations != null && c.Allocations.Any(a => a.VendorID == Auth.VendorID.Value))).ToList();

                foreach (var c in ActiveContracts)
                {
                    if (string.IsNullOrWhiteSpace(c.OutletName) && OutletDictionary.TryGetValue(c.OutletID, out var oName))
                    {
                        c.OutletName = oName;
                    }
                    if (c.ProductID > 0 && !string.IsNullOrWhiteSpace(c.ProductName) && !c.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
                    {
                        ProductDictionary[c.ProductID] = c.ProductName;
                    }
                    else if (string.IsNullOrWhiteSpace(c.ProductName) && ProductDictionary.TryGetValue(c.ProductID, out var pName))
                    {
                        c.ProductName = pName;
                    }
                }
            }

            // 7. Load Purchase Requests & Cache
            try
            {
                var prs = await Api.GetPurchaseRequestsAsync();
                AllPurchaseRequests = prs ?? new List<PurchaseRequestDto>();
                foreach (var pr in AllPurchaseRequests)
                {
                    RequestCache[pr.RequestID] = pr;
                    if (pr.Items != null)
                    {
                        foreach (var item in pr.Items)
                        {
                            if (!string.IsNullOrWhiteSpace(item.ProductName) && !item.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
                            {
                                ProductDictionary[item.ProductID] = item.ProductName;
                            }
                        }
                    }
                }
            }
            catch { }

            // 7.5. Load Purchase Orders for this Vendor
            try
            {
                var allPos = await Api.GetPurchaseOrdersAsync();
                if (allPos != null && Auth.VendorID.HasValue)
                {
                    MyPurchaseOrders = allPos
                        .Where(p => p.VendorID == Auth.VendorID.Value)
                        .OrderByDescending(p => p.PurchaseOrderID)
                        .ToList();
                }
            }
            catch { }

            // 7.6. Load Invoices for this Vendor
            try
            {
                MyInvoices = await Api.GetInvoicesAsync() ?? new List<InvoiceDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VendorDashboard] Error loading invoices: {ex.Message}");
            }

            // 8. Load Individual Purchase Requests for Quotations & Contracts if not yet cached
            foreach (var q in AllQuotations)
            {
                if (!RequestCache.ContainsKey(q.RequestID))
                {
                    try
                    {
                        var pr = await Api.GetPurchaseRequestByIdAsync(q.RequestID);
                        if (pr != null)
                        {
                            RequestCache[q.RequestID] = pr;
                            if (pr.Items != null)
                            {
                                foreach (var item in pr.Items)
                                {
                                    if (!string.IsNullOrWhiteSpace(item.ProductName) && !item.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
                                    {
                                        ProductDictionary[item.ProductID] = item.ProductName;
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            foreach (var c in ActiveContracts)
            {
                if (c.RequestID.HasValue && !RequestCache.ContainsKey(c.RequestID.Value))
                {
                    try
                    {
                        var pr = await Api.GetPurchaseRequestByIdAsync(c.RequestID.Value);
                        if (pr != null)
                        {
                            RequestCache[c.RequestID.Value] = pr;
                        }
                    }
                    catch { }
                }
            }

            // 9. Load Notifications
            try
            {
                Notifications = await Api.GetMyNotificationsAsync() ?? new List<NotificationDto>();
            }
            catch { }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VendorDashboard] Error loading data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private string GetGreeting()
    {
        var hour = DateTime.Now.Hour;
        if (hour < 12) return "Good morning";
        if (hour < 17) return "Good afternoon";
        return "Good evening";
    }

    private void SetSidebarNav(string nav)
    {
        ActiveSidebarNav = nav;
        SelectedQuotation = null;
        SelectedQuotationPR = null;
        SelectedContract = null;
        SearchQuery = string.Empty;
        ShowNotificationDropdown = false;

        if (nav == "Dashboard")
        {
            ActiveTab = "new";
        }
        else if (nav == "MyQuotations")
        {
            QuotationStatusFilter = "All";
        }
        else if (nav == "MyContracts")
        {
            ContractStatusFilter = "Active";
        }

        StateHasChanged();
    }

    private void SetTab(string tab)
    {
        ActiveTab = tab;
        ShowNotificationDropdown = false;

        if (tab == "contracts")
        {
            ActiveSidebarNav = "MyContracts";
            ContractStatusFilter = "Active";
        }
        else if (tab == "pending" || tab == "accepted" || tab == "rejected")
        {
            ActiveSidebarNav = "MyQuotations";
            QuotationStatusFilter = tab == "pending" ? "Pending" : tab == "accepted" ? "Accepted" : "Rejected";
        }
        else
        {
            ActiveSidebarNav = "Dashboard";
        }

        StateHasChanged();
    }

    private void SetQuotationFilter(string filter)
    {
        QuotationStatusFilter = filter;
        StateHasChanged();
    }

    private void SetContractFilter(string filter)
    {
        ContractStatusFilter = filter;
        StateHasChanged();
    }

    private void OpenQuotationDetails(int quotationId)
    {
        SelectedQuotation = AllQuotations.FirstOrDefault(q => q.QuotationID == quotationId);
        if (SelectedQuotation != null)
        {
            if (RequestCache.TryGetValue(SelectedQuotation.RequestID, out var cachedPR))
            {
                SelectedQuotationPR = cachedPR;
            }
            else
            {
                SelectedQuotationPR = AllPurchaseRequests.FirstOrDefault(pr => pr.RequestID == SelectedQuotation.RequestID);
            }
            ActiveSidebarNav = "QuotationDetails";
        }
        StateHasChanged();
    }

    private void CloseQuotationDetails()
    {
        SelectedQuotation = null;
        SelectedQuotationPR = null;
        ActiveSidebarNav = "MyQuotations";
        StateHasChanged();
    }

    private void OpenContractDetails(int contractId)
    {
        SelectedContract = ActiveContracts.FirstOrDefault(c => c.ContractID == contractId);
        if (SelectedContract != null)
        {
            ActiveSidebarNav = "ContractDetails";
        }
        StateHasChanged();
    }

    private void CloseContractDetails()
    {
        SelectedContract = null;
        ActiveSidebarNav = "MyContracts";
        StateHasChanged();
    }

    private string GetOutletName(QuotationDto q)
    {
        // 1. Check in Opportunities list
        var opp = Opportunities?.FirstOrDefault(o => o.RequestID == q.RequestID);
        if (opp != null && !string.IsNullOrWhiteSpace(opp.OutletName))
        {
            return opp.OutletName;
        }

        // 2. Check in Active Contracts list
        var contract = ActiveContracts?.FirstOrDefault(c => c.QuotationID == q.QuotationID);
        if (contract != null && !string.IsNullOrWhiteSpace(contract.OutletName))
        {
            return contract.OutletName;
        }

        // 3. Check in RequestCache or AllPurchaseRequests
        PurchaseRequestDto? pr = null;
        if (RequestCache.TryGetValue(q.RequestID, out var cachedPR))
        {
            pr = cachedPR;
        }
        else
        {
            pr = AllPurchaseRequests.FirstOrDefault(p => p.RequestID == q.RequestID);
        }

        if (pr != null)
        {
            if (OutletDictionary.TryGetValue(pr.OutletID, out var name) && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
            if (!string.IsNullOrWhiteSpace(pr.OutletName))
            {
                return pr.OutletName;
            }
        }

        // 4. Default to first opportunity outlet name or intelligent context
        var firstOpp = Opportunities?.FirstOrDefault();
        if (firstOpp != null && !string.IsNullOrWhiteSpace(firstOpp.OutletName))
        {
            return firstOpp.OutletName;
        }

        return "Outlet";
    }

    private string GetOrgName(QuotationDto q)
    {
        // 1. Check in Opportunities list
        var opp = Opportunities?.FirstOrDefault(o => o.RequestID == q.RequestID);
        if (opp != null && !string.IsNullOrWhiteSpace(opp.OrganizationName))
        {
            return opp.OrganizationName;
        }

        // 2. Check in RequestCache for OutletID -> Organization
        PurchaseRequestDto? pr = null;
        if (RequestCache.TryGetValue(q.RequestID, out var cachedPR))
        {
            pr = cachedPR;
        }
        else
        {
            pr = AllPurchaseRequests.FirstOrDefault(p => p.RequestID == q.RequestID);
        }

        if (pr != null && OutletOrgMap.TryGetValue(pr.OutletID, out var orgId) && OrgDictionary.TryGetValue(orgId, out var orgName))
        {
            return orgName;
        }

        // 3. Check first opportunity organization name
        var firstOpp = Opportunities?.FirstOrDefault();
        if (firstOpp != null && !string.IsNullOrWhiteSpace(firstOpp.OrganizationName))
        {
            return firstOpp.OrganizationName;
        }

        // 4. Check OrgDictionary
        var firstOrg = OrgDictionary.Values.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstOrg))
        {
            return firstOrg;
        }

        return "Organization";
    }

    private string GetContractOutletName(ContractDto c)
    {
        // 1. Direct field on contract
        if (!string.IsNullOrWhiteSpace(c.OutletName))
        {
            return c.OutletName;
        }

        // 2. Lookup via OutletDictionary using OutletID
        if (c.OutletID > 0 && OutletDictionary.TryGetValue(c.OutletID, out var oName) && !string.IsNullOrWhiteSpace(oName))
        {
            return oName;
        }

        // 3. Lookup via RequestID
        if (c.RequestID.HasValue)
        {
            var opp = Opportunities?.FirstOrDefault(o => o.RequestID == c.RequestID.Value);
            if (opp != null && !string.IsNullOrWhiteSpace(opp.OutletName))
            {
                return opp.OutletName;
            }

            if (RequestCache.TryGetValue(c.RequestID.Value, out var pr) && !string.IsNullOrWhiteSpace(pr.OutletName))
            {
                return pr.OutletName;
            }
        }

        // 4. Lookup via QuotationID
        if (c.QuotationID.HasValue)
        {
            var q = AllQuotations.FirstOrDefault(qt => qt.QuotationID == c.QuotationID.Value);
            if (q != null)
            {
                return GetOutletName(q);
            }
        }

        // 5. Intelligent match from Opportunities
        var matchingOpp = Opportunities?.FirstOrDefault(o => o.ProductID == c.ProductID);
        if (matchingOpp != null && !string.IsNullOrWhiteSpace(matchingOpp.OutletName))
        {
            return matchingOpp.OutletName;
        }

        var firstOpp = Opportunities?.FirstOrDefault();
        if (firstOpp != null && !string.IsNullOrWhiteSpace(firstOpp.OutletName))
        {
            return firstOpp.OutletName;
        }

        return "Outlet";
    }

    private string GetContractOrgName(ContractDto c)
    {
        // 1. Direct field on contract
        if (!string.IsNullOrWhiteSpace(c.OrganizationName))
        {
            return c.OrganizationName;
        }

        // 2. Lookup via OrganizationID
        if (c.OrganizationID > 0 && OrgDictionary.TryGetValue(c.OrganizationID, out var orgName) && !string.IsNullOrWhiteSpace(orgName))
        {
            return orgName;
        }

        // 3. Lookup via OutletID -> OrganizationID
        if (c.OutletID > 0 && OutletOrgMap.TryGetValue(c.OutletID, out var oOrgId) && OrgDictionary.TryGetValue(oOrgId, out var oOrgName))
        {
            return oOrgName;
        }

        // 4. Lookup via RequestID in Opportunities
        if (c.RequestID.HasValue)
        {
            var opp = Opportunities?.FirstOrDefault(o => o.RequestID == c.RequestID.Value);
            if (opp != null && !string.IsNullOrWhiteSpace(opp.OrganizationName))
            {
                return opp.OrganizationName;
            }
        }

        // 5. Lookup via QuotationID
        if (c.QuotationID.HasValue)
        {
            var q = AllQuotations.FirstOrDefault(qt => qt.QuotationID == c.QuotationID.Value);
            if (q != null)
            {
                return GetOrgName(q);
            }
        }

        var firstOrg = OrgDictionary.Values.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstOrg))
        {
            return firstOrg;
        }

        return "Organization";
    }

    private string GetContractProductName(ContractDto c)
    {
        if (!string.IsNullOrWhiteSpace(c.ProductName) && !c.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
        {
            return c.ProductName;
        }

        return GetProductName(c.ProductID, c.RequestID ?? 0, c.QuotationID ?? 0);
    }

    private string GetContractUnit(ContractDto c)
    {
        if (!string.IsNullOrWhiteSpace(c.Unit))
        {
            return c.Unit;
        }

        if (c.RequestID.HasValue)
        {
            var opp = Opportunities?.FirstOrDefault(o => o.RequestID == c.RequestID.Value);
            if (opp != null && !string.IsNullOrWhiteSpace(opp.Unit))
            {
                return opp.Unit;
            }
        }

        return "Kg";
    }

    private double GetUsedPercent(ContractDto c)
    {
        if (c.TotalQuantity <= 0) return 0;
        var pct = (double)(c.UsedQuantity / c.TotalQuantity) * 100;
        return Math.Min(100, Math.Max(0, Math.Round(pct, 0)));
    }

    private double GetRemainingPercent(ContractDto c)
    {
        if (c.TotalQuantity <= 0) return 100;
        var rem = c.TotalQuantity - c.UsedQuantity;
        var pct = (double)(rem / c.TotalQuantity) * 100;
        return Math.Min(100, Math.Max(0, Math.Round(pct, 0)));
    }

    private string GetProductName(QuotationDto q)
    {
        var firstItem = q.Items?.FirstOrDefault();
        int pId = firstItem?.ProductID ?? 0;
        return GetProductName(pId, q.RequestID, q.QuotationID);
    }

    private string GetProductName(int productId, int requestId = 0, int quotationId = 0)
    {
        // 1. Check ProductDictionary
        if (productId > 0 && ProductDictionary.TryGetValue(productId, out var pName) && !string.IsNullOrWhiteSpace(pName) && !pName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
        {
            return pName;
        }

        // 2. Check Opportunities list by RequestID or ProductID
        if (requestId > 0)
        {
            var opp = Opportunities?.FirstOrDefault(o => o.RequestID == requestId);
            if (opp != null && !string.IsNullOrWhiteSpace(opp.ProductName) && !opp.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
            {
                if (productId > 0) ProductDictionary[productId] = opp.ProductName;
                return opp.ProductName;
            }
        }

        if (productId > 0)
        {
            var opp = Opportunities?.FirstOrDefault(o => o.ProductID == productId);
            if (opp != null && !string.IsNullOrWhiteSpace(opp.ProductName) && !opp.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
            {
                ProductDictionary[productId] = opp.ProductName;
                return opp.ProductName;
            }
        }

        // 3. Check Contracts list by QuotationID or ProductID
        if (quotationId > 0)
        {
            var contract = ActiveContracts?.FirstOrDefault(c => c.QuotationID == quotationId);
            if (contract != null && !string.IsNullOrWhiteSpace(contract.ProductName) && !contract.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
            {
                if (productId > 0) ProductDictionary[productId] = contract.ProductName;
                return contract.ProductName;
            }
        }

        if (productId > 0)
        {
            var contract = ActiveContracts?.FirstOrDefault(c => c.ProductID == productId);
            if (contract != null && !string.IsNullOrWhiteSpace(contract.ProductName) && !contract.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
            {
                ProductDictionary[productId] = contract.ProductName;
                return contract.ProductName;
            }
        }

        // 4. Check RequestCache
        if (requestId > 0 && RequestCache.TryGetValue(requestId, out var pr) && pr.Items != null)
        {
            var prItem = pr.Items.FirstOrDefault(i => productId <= 0 || i.ProductID == productId) ?? pr.Items.FirstOrDefault();
            if (prItem != null && !string.IsNullOrWhiteSpace(prItem.ProductName) && !prItem.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
            {
                if (productId > 0) ProductDictionary[productId] = prItem.ProductName;
                return prItem.ProductName;
            }
        }

        // 5. Check first opportunity product name
        var topOpp = Opportunities?.FirstOrDefault();
        if (topOpp != null && !string.IsNullOrWhiteSpace(topOpp.ProductName) && !topOpp.ProductName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
        {
            return topOpp.ProductName;
        }

        if (productId > 0)
        {
            return $"Product #{productId}";
        }

        return "Product Item";
    }

    private string GetQuantityDisplay(QuotationDto q)
    {
        if (q.Items != null && q.Items.Count > 0)
        {
            var totalQty = q.Items.Sum(i => i.Quantity);
            var opp = Opportunities?.FirstOrDefault(o => o.RequestID == q.RequestID);
            var unit = opp?.Unit;
            if (string.IsNullOrWhiteSpace(unit) && RequestCache.TryGetValue(q.RequestID, out var pr) && pr.Items != null)
            {
                unit = pr.Items.FirstOrDefault()?.Unit;
            }
            if (string.IsNullOrWhiteSpace(unit))
            {
                unit = "Kg";
            }
            return $"{totalQty:N2} {unit}";
        }
        return "0.00 units";
    }

    private decimal GetQuotationTotal(QuotationDto q)
    {
        if (q.Items != null && q.Items.Count > 0)
        {
            return q.Items.Sum(i => i.TotalAmount > 0 ? i.TotalAmount : (i.Quantity * i.UnitPrice));
        }
        return 0m;
    }

    private void ToggleNotifications()
    {
        ShowNotificationDropdown = !ShowNotificationDropdown;
    }

    private async Task MarkAsRead(int notificationId)
    {
        try
        {
            await Api.MarkNotificationReadAsync(notificationId);
            var notif = Notifications.FirstOrDefault(n => n.NotificationID == notificationId);
            if (notif != null) notif.IsRead = true;
            StateHasChanged();
        }
        catch { }
    }

    private async Task HandleVendorNotificationClick(NotificationDto notif)
    {
        await MarkAsRead(notif.NotificationID);
        ShowNotificationDropdown = false;

        if (notif.NotificationType == "VendorReview")
        {
            await OpenReviewFromNotification(notif.RelatedRequestID ?? 0);
        }
        else if (notif.NotificationType == "PaymentReceived" || notif.Title.Contains("Payment", StringComparison.OrdinalIgnoreCase))
        {
            SetSidebarNav("Invoices");
        }
        else if (notif.NotificationType == "PurchaseOrderDelivered" || notif.NotificationType == "PurchaseOrderDispatched" || notif.Title.Contains("Purchase Order", StringComparison.OrdinalIgnoreCase))
        {
            SetSidebarNav("PurchaseOrders");
        }
        else if (notif.RelatedRequestID.HasValue)
        {
            ViewOpportunity(notif.RelatedRequestID.Value);
        }
    }

    private async Task RespondToPo(int poId, string status)
    {
        if (!Auth.VendorID.HasValue) return;
        try
        {
            var cmd = new RespondToPurchaseOrderCommand
            {
                PurchaseOrderID = poId,
                VendorID = Auth.VendorID.Value,
                Status = status
            };
            var result = await Api.RespondToPurchaseOrderAsync(cmd);
            if (result != null)
            {
                var po = MyPurchaseOrders.FirstOrDefault(p => p.PurchaseOrderID == poId);
                if (po != null) po.Status = status;
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VendorDashboard] RespondToPo error: {ex.Message}");
        }
    }

    private async Task OpenDispatchModal(PurchaseOrderDto po)
    {
        SelectedPoForDispatch = po;
        ShowDispatchModal = true;
        SpoilageAdvisorData = null;
        SpoilageAdviceError = null;
        IsLoadingSpoilageAdvice = true;
        IsDispatchingPo = false;
        StateHasChanged();

        try
        {
            var res = await Api.GetSpoilageAdviceAsync(po.PurchaseOrderID);
            if (res?.Advisor != null)
            {
                SpoilageAdvisorData = res.Advisor;
            }
            else
            {
                SpoilageAdviceError = "AI advice is currently unavailable. You may proceed with dispatch as normal.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VendorDashboard] OpenDispatchModal error: {ex.Message}");
            SpoilageAdviceError = "AI advice is currently unavailable. You may proceed with dispatch as normal.";
        }
        finally
        {
            IsLoadingSpoilageAdvice = false;
            StateHasChanged();
        }
    }

    private void CloseDispatchModal()
    {
        ShowDispatchModal = false;
        SelectedPoForDispatch = null;
        SpoilageAdvisorData = null;
        SpoilageAdviceError = null;
        IsLoadingSpoilageAdvice = false;
        IsDispatchingPo = false;
        StateHasChanged();
    }

    private async Task DispatchPo(int poId)
    {
        if (!Auth.VendorID.HasValue) return;
        try
        {
            IsDispatchingPo = true;
            StateHasChanged();

            var cmd = new DispatchPurchaseOrderCommand
            {
                PurchaseOrderID = poId,
                VendorID = Auth.VendorID.Value
            };
            var result = await Api.DispatchPurchaseOrderAsync(cmd);
            if (result != null)
            {
                var po = MyPurchaseOrders.FirstOrDefault(p => p.PurchaseOrderID == poId);
                if (po != null)
                {
                    po.Status = "Dispatched";
                    po.DispatchDateTime = result.DispatchDateTime;
                }
                CloseDispatchModal();
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VendorDashboard] DispatchPo error: {ex.Message}");
        }
        finally
        {
            IsDispatchingPo = false;
            StateHasChanged();
        }
    }

    private void ViewOpportunity(int requestId)
    {
        Nav.NavigateTo($"/vendor/procurement/{requestId}");
    }

    private async Task OpenCreateInvoiceModal(PurchaseOrderDto po)
    {
        SelectedPoForInvoice = po;
        InvoiceSubmitError = string.Empty;
        InvoiceSubmitSuccess = string.Empty;
        IsLoadingDeliveryData = true;
        ShowCreateInvoiceModal = true;
        StateHasChanged();

        try
        {
            SelectedPoDeliveryRecords = await Api.GetDeliveryRecordsByPurchaseOrderAsync(po.PurchaseOrderID) ?? new List<DeliveryRecordDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VendorDashboard] Error loading delivery records: {ex.Message}");
            SelectedPoDeliveryRecords = new List<DeliveryRecordDto>();
        }
        finally
        {
            IsLoadingDeliveryData = false;
            StateHasChanged();
        }
    }

    private void CloseCreateInvoiceModal()
    {
        ShowCreateInvoiceModal = false;
        SelectedPoForInvoice = null;
        SelectedPoDeliveryRecords.Clear();
        InvoiceSubmitError = string.Empty;
        InvoiceSubmitSuccess = string.Empty;
    }

    private decimal GetDeliveredQuantity(PurchaseOrderItemDto item)
    {
        if (SelectedPoDeliveryRecords == null || SelectedPoDeliveryRecords.Count == 0) return item.Quantity;
        var dr = SelectedPoDeliveryRecords.FirstOrDefault(d => d.POItemID == item.POItemID && string.Equals(d.Status, "Confirmed", StringComparison.OrdinalIgnoreCase));
        return dr != null ? dr.ReceivedQuantity : item.Quantity;
    }

    private decimal GetSpoiledQuantity(PurchaseOrderItemDto item)
    {
        if (SelectedPoDeliveryRecords == null || SelectedPoDeliveryRecords.Count == 0) return 0;
        var dr = SelectedPoDeliveryRecords.FirstOrDefault(d => d.POItemID == item.POItemID && string.Equals(d.Status, "Confirmed", StringComparison.OrdinalIgnoreCase));
        return dr != null ? dr.SpoiledQuantity : 0;
    }

    private decimal GetNetAcceptedQuantity(PurchaseOrderItemDto item)
    {
        var received = GetDeliveredQuantity(item);
        var spoiled = GetSpoiledQuantity(item);
        var net = received - spoiled;
        return net < 0 ? 0 : net;
    }

    private decimal GetItemNetTotal(PurchaseOrderItemDto item)
    {
        var netQty = GetNetAcceptedQuantity(item);
        var subtotal = Math.Round(netQty * item.UnitPrice, 2);
        var tax = Math.Round(subtotal * (item.TaxRate / 100m), 2);
        return subtotal + tax;
    }

    private decimal GetInvoiceSubtotal()
    {
        if (SelectedPoForInvoice?.Items == null) return 0;
        return SelectedPoForInvoice.Items.Sum(item => Math.Round(GetNetAcceptedQuantity(item) * item.UnitPrice, 2));
    }

    private decimal GetInvoiceTaxTotal()
    {
        if (SelectedPoForInvoice?.Items == null) return 0;
        return SelectedPoForInvoice.Items.Sum(item =>
        {
            var subtotal = Math.Round(GetNetAcceptedQuantity(item) * item.UnitPrice, 2);
            return Math.Round(subtotal * (item.TaxRate / 100m), 2);
        });
    }

    private decimal GetInvoiceGrandTotal()
    {
        return GetInvoiceSubtotal() + GetInvoiceTaxTotal();
    }

    private async Task HandleSubmitInvoice()
    {
        if (SelectedPoForInvoice == null || !Auth.VendorID.HasValue) return;

        IsSubmittingInvoice = true;
        InvoiceSubmitError = string.Empty;
        InvoiceSubmitSuccess = string.Empty;

        try
        {
            var result = await Api.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                PurchaseOrderID = SelectedPoForInvoice.PurchaseOrderID,
                VendorID = Auth.VendorID.Value
            });

            if (result.Success && result.Data != null)
            {
                InvoiceSubmitSuccess = $"Invoice INV-{result.Data.InvoiceID} submitted successfully!";
                MyInvoices = await Api.GetInvoicesAsync() ?? new List<InvoiceDto>();
                await Task.Delay(1200);
                CloseCreateInvoiceModal();
                InvoiceSubTab = "History";
            }
            else
            {
                InvoiceSubmitError = result.ErrorMessage ?? "Failed to create invoice. Please verify delivery status.";
            }
        }
        catch (Exception ex)
        {
            InvoiceSubmitError = $"Error submitting invoice: {ex.Message}";
        }
        finally
        {
            IsSubmittingInvoice = false;
            StateHasChanged();
        }
    }

    private async Task HandleDownloadInvoicePdf(InvoiceDto invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvoiceDocumentBase64))
        {
            var full = await Api.GetInvoiceByIdAsync(invoice.InvoiceID);
            if (full != null && !string.IsNullOrWhiteSpace(full.InvoiceDocumentBase64))
            {
                invoice = full;
            }
        }

        if (!string.IsNullOrWhiteSpace(invoice.InvoiceDocumentBase64))
        {
            string fileName = string.IsNullOrWhiteSpace(invoice.InvoiceFileName) ? $"Invoice-{invoice.InvoiceID}.pdf" : invoice.InvoiceFileName;
            await JS.InvokeVoidAsync("downloadFileFromBase64", fileName, invoice.InvoiceContentType ?? "application/pdf", invoice.InvoiceDocumentBase64);
        }
    }

    private async Task HandleViewInvoicePdf(InvoiceDto invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvoiceDocumentBase64))
        {
            var full = await Api.GetInvoiceByIdAsync(invoice.InvoiceID);
            if (full != null && !string.IsNullOrWhiteSpace(full.InvoiceDocumentBase64))
            {
                invoice = full;
            }
        }

        if (!string.IsNullOrWhiteSpace(invoice.InvoiceDocumentBase64))
        {
            await JS.InvokeVoidAsync("viewPdfFromBase64", invoice.InvoiceDocumentBase64);
        }
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login");
    }

    private async Task OpenReviewFromNotification(int feedbackId)
    {
        ReviewLoadErrorMessage = null;
        SelectedReviewDetails = null;

        if (feedbackId <= 0)
        {
            ReviewLoadErrorMessage = "Unable to load this review.";
            ShowReviewDetailsModal = true;
            StateHasChanged();
            return;
        }

        try
        {
            var review = await Api.GetVendorFeedbackByIdAsync(feedbackId);
            if (review == null)
            {
                ReviewLoadErrorMessage = "Unable to load this review.";
            }
            else
            {
                SelectedReviewDetails = review;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VendorDashboard] Error loading review {feedbackId}: {ex.Message}");
            ReviewLoadErrorMessage = "Unable to load this review.";
        }

        ShowReviewDetailsModal = true;
        StateHasChanged();
    }

    private void OpenReviewDetails(VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto rev)
    {
        SelectedReviewDetails = rev;
        ReviewLoadErrorMessage = null;
        ShowReviewDetailsModal = true;
    }

    private void CloseReviewDetailsModal()
    {
        ShowReviewDetailsModal = false;
        SelectedReviewDetails = null;
        ReviewLoadErrorMessage = null;
    }
}
