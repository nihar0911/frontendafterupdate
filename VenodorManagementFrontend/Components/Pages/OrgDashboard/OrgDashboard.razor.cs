using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.OrgDashboard;

public partial class OrgDashboard : ComponentBase
{
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;

    private string OrganizationName { get; set; } = string.Empty;
    private int PurchaseRequestsCount { get; set; }
    private int PendingQuotationsCount { get; set; }
    private int ActiveContractsCount { get; set; }
    private int? PurchaseOrdersCount { get; set; }
    private int OutletsCount { get; set; }

    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private List<OutletDto> OrganizationOutletsList { get; set; } = new();
    private OutletDto? SelectedOutletForModal { get; set; }
    private List<QuotationDto> PendingQuotationsList { get; set; } = new();
    private List<ContractDto> ActiveContractsList { get; set; } = new();
    private Dictionary<int, string> VendorNames { get; set; } = new();
    private Dictionary<int, string> ProductNames { get; set; } = new();
    private Dictionary<int, string> OutletNames { get; set; } = new();
    private Dictionary<int, PurchaseRequestDto> PurchaseRequestDict { get; set; } = new();
    private List<PurchaseRequestDto> OrgPurchaseRequestsList { get; set; } = new();

    private List<NotificationDto> Notifications { get; set; } = new();
    private int UnreadNotificationCount => Notifications.Count(n => !n.IsRead);
    private bool ShowNotificationDropdown { get; set; } = false;

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ToggleProfileDropdown()
    {
        IsProfileDropdownOpen = !IsProfileDropdownOpen;
        if (IsProfileDropdownOpen)
        {
            ShowNotificationDropdown = false;
        }
    }

    private void ToggleNotifications()
    {
        ShowNotificationDropdown = !ShowNotificationDropdown;
        if (ShowNotificationDropdown)
        {
            IsProfileDropdownOpen = false;
        }
    }

    private async Task LoadNotifications()
    {
        try
        {
            Notifications = await Api.GetMyNotificationsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrgDashboard] Error loading notifications: {ex.Message}");
        }
    }

    private async Task MarkAsRead(int notificationId)
    {
        await Api.MarkNotificationReadAsync(notificationId);
        var notif = Notifications.FirstOrDefault(n => n.NotificationID == notificationId);
        if (notif != null)
        {
            notif.IsRead = true;
        }
        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        if (Auth.IsAuthenticated && (Auth.IsOrgManager || Auth.IsAdmin))
        {
            await LoadDashboardData();
        }
        else
        {
            IsLoading = false;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Auth.IsAuthenticated && (Auth.IsOrgManager || Auth.IsAdmin))
        {
            await LoadDashboardData();
        }
    }

    private async Task LoadDashboardData()
    {
        IsLoading = true;
        HasError = false;
        StateHasChanged();

        try
        {
            var orgsTask = Api.GetOrganizationsAsync();
            var outletsTask = Api.GetOutletsAsync();
            var requestsTask = Api.GetPurchaseRequestsAsync();
            var quotationsTask = Api.GetQuotationsAsync();
            var contractsTask = Api.GetContractsAsync();
            var ordersTask = Api.GetPurchaseOrdersAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var productsTask = Api.GetProductsAsync();

            await Task.WhenAll(orgsTask, outletsTask, requestsTask, quotationsTask, contractsTask, ordersTask, vendorsTask, productsTask);

            var orgs = await orgsTask;
            if (Auth.OrganizationID.HasValue && orgs != null)
            {
                var matchingOrg = orgs.FirstOrDefault(o => o.OrganizationID == Auth.OrganizationID.Value);
                OrganizationName = matchingOrg?.OrganizationName ?? $"Organization #{Auth.OrganizationID.Value}";
            }
            else if (orgs != null && orgs.Count > 0)
            {
                OrganizationName = orgs[0].OrganizationName;
            }
            else
            {
                OrganizationName = "N/A";
            }

            var outlets = await outletsTask;
            if (outlets != null)
            {
                foreach (var o in outlets)
                {
                    if (string.IsNullOrWhiteSpace(o.OutletName))
                    {
                        o.OutletName = !string.IsNullOrWhiteSpace(o.Address) 
                            ? $"{o.Address.Split(',')[0].Trim()} Outlet" 
                            : $"Outlet #{o.OutletID}";
                    }
                }
                OutletNames = outlets.ToDictionary(o => o.OutletID, o => o.OutletName);
                if (Auth.OrganizationID.HasValue && Auth.OrganizationID.Value > 0)
                {
                    OrganizationOutletsList = outlets.Where(o => o.OrganizationID == Auth.OrganizationID.Value).ToList();
                }
                else
                {
                    OrganizationOutletsList = outlets.ToList();
                }
            }
            else
            {
                OrganizationOutletsList = new List<OutletDto>();
            }
            OutletsCount = OrganizationOutletsList.Count;

            var orgOutletIDs = OrganizationOutletsList.Select(o => o.OutletID).ToHashSet();
            bool isScopedToOrg = Auth.OrganizationID.HasValue && Auth.OrganizationID.Value > 0;

            var requests = await requestsTask;
            if (requests != null)
            {
                PurchaseRequestDict = requests.ToDictionary(r => r.RequestID, r => r);
                OrgPurchaseRequestsList = isScopedToOrg
                    ? (orgOutletIDs.Count > 0 ? requests.Where(r => orgOutletIDs.Contains(r.OutletID)).ToList() : new List<PurchaseRequestDto>())
                    : requests.ToList();
            }
            PurchaseRequestsCount = OrgPurchaseRequestsList.Count;

            var quotations = await quotationsTask;
            var pendingQuotationItems = quotations?.Where(q => 
                (string.Equals(q.Status, "Submitted", StringComparison.OrdinalIgnoreCase) || string.Equals(q.Status, "Pending", StringComparison.OrdinalIgnoreCase)) &&
                (isScopedToOrg 
                    ? (orgOutletIDs.Count > 0 && PurchaseRequestDict.TryGetValue(q.RequestID, out var pr) && orgOutletIDs.Contains(pr.OutletID))
                    : true)
            ).ToList() ?? new List<QuotationDto>();
            PendingQuotationsCount = pendingQuotationItems.Count;
            PendingQuotationsList = pendingQuotationItems;

            var contracts = await contractsTask;
            var activeContracts = contracts?.Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase)).ToList() ?? new List<ContractDto>();
            if (isScopedToOrg)
            {
                ActiveContractsList = orgOutletIDs.Count > 0
                    ? activeContracts.Where(c => orgOutletIDs.Contains(c.OutletID)).ToList()
                    : new List<ContractDto>();
            }
            else
            {
                ActiveContractsList = activeContracts;
            }

            var vendors = await vendorsTask;
            if (vendors != null)
            {
                VendorNames = vendors.ToDictionary(v => v.VendorID, v => v.VendorName);
            }

            var products = await productsTask;
            if (products != null)
            {
                ProductNames = products.ToDictionary(p => p.ProductID, p => p.ProductName);
            }

            // Enrich all contracts with human-readable names
            foreach (var c in ActiveContractsList)
            {
                if (string.IsNullOrWhiteSpace(c.OutletName) && OutletNames.TryGetValue(c.OutletID, out var oName))
                {
                    c.OutletName = oName;
                }
                if (string.IsNullOrWhiteSpace(c.VendorName))
                {
                    int vId = c.VendorID ?? c.Allocations?.FirstOrDefault()?.VendorID ?? 0;
                    if (vId > 0 && VendorNames.TryGetValue(vId, out var vName))
                    {
                        c.VendorName = vName;
                    }
                }
                if (string.IsNullOrWhiteSpace(c.ProductName) && ProductNames.TryGetValue(c.ProductID, out var pName))
                {
                    c.ProductName = pName;
                }
            }

            ActiveContractsCount = ActiveContractsList.Count;

            var orders = await ordersTask;
            PurchaseOrdersCount = orders?.Count;

            await LoadNotifications();
        }
        catch (Exception)
        {
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void NavigateToContracts()
    {
        Nav.NavigateTo("/organization/contracts");
    }

    private void OpenOutletDetails(OutletDto outlet)
    {
        SelectedOutletForModal = outlet;
    }

    private void CloseOutletDetails()
    {
        SelectedOutletForModal = null;
    }

    private int GetOutletPrCount(int outletId)
    {
        return OrgPurchaseRequestsList.Count(r => r.OutletID == outletId);
    }

    private int GetOutletContractCount(int outletId)
    {
        return ActiveContractsList.Count(c => c.OutletID == outletId);
    }

    private string GetVendorName(int vendorId)
    {
        if (VendorNames.TryGetValue(vendorId, out var name) && !string.IsNullOrWhiteSpace(name)) return name;
        return $"Vendor #{vendorId}";
    }

    private string GetProductName(int productId)
    {
        if (ProductNames.TryGetValue(productId, out var name) && !string.IsNullOrWhiteSpace(name)) return name;
        return $"Product #{productId}";
    }

    private string GetOutletName(int outletId)
    {
        if (OutletNames.TryGetValue(outletId, out var name)) return name;
        return outletId > 0 ? $"Outlet #{outletId}" : "N/A";
    }

    private int GetPrOutletId(int requestId)
    {
        if (PurchaseRequestDict.TryGetValue(requestId, out var pr)) return pr.OutletID;
        return 0;
    }

    private string GetUserInitial()
    {
        if (!string.IsNullOrWhiteSpace(Auth.UserName))
        {
            return Auth.UserName.Substring(0, 1).ToUpperInvariant();
        }
        return "O";
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login");
    }
}