using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.PurchaseRequestDetails;

public partial class PurchaseRequestDetails : ComponentBase
{
    [Parameter] public int? requestId { get; set; }

    private PurchaseRequestDto? PurchaseRequest { get; set; }
    private QuotationDto? AssociatedQuotation { get; set; }

    private string OutletName { get; set; } = string.Empty;
    private string OutletAddress { get; set; } = string.Empty;
    private string OrganizationName { get; set; } = string.Empty;
    private string CreatedByUserName { get; set; } = string.Empty;
    private string VendorName { get; set; } = string.Empty;
    private Dictionary<int, string> ProductNames { get; set; } = new();

    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;
    private string ErrorMessage { get; set; } = "You are not authorized to view this purchase request or it does not exist.";

    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;
    private bool ShowNotificationDropdown { get; set; } = false;
    private bool ShowProfileModal { get; set; } = false;

    private List<NotificationDto> Notifications { get; set; } = new();
    private int UnreadNotificationCount => Notifications.Count(n => !n.IsRead);

    private string UserInitial => !string.IsNullOrWhiteSpace(Auth.UserName)
        ? Auth.UserName.Substring(0, 1).ToUpperInvariant()
        : "P";

    protected override async Task OnInitializedAsync()
    {
        if (requestId.HasValue && requestId.Value > 0)
        {
            await LoadDetails(requestId.Value);
        }
        else
        {
            IsLoading = false;
            HasError = true;
            ErrorMessage = "No Purchase Request ID specified.";
        }
    }

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ToggleProfileDropdown()
    {
        IsProfileDropdownOpen = !IsProfileDropdownOpen;
        if (IsProfileDropdownOpen) ShowNotificationDropdown = false;
    }

    private void ToggleNotifications()
    {
        ShowNotificationDropdown = !ShowNotificationDropdown;
        if (ShowNotificationDropdown) IsProfileDropdownOpen = false;
    }

    private void OpenProfileModal()
    {
        ShowProfileModal = true;
        IsProfileDropdownOpen = false;
    }

    private void CloseProfileModal()
    {
        ShowProfileModal = false;
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login", true);
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

    private async Task MarkAllAsRead()
    {
        var success = await Api.MarkAllNotificationsReadAsync();
        if (success)
        {
            if (Notifications != null)
            {
                foreach (var notif in Notifications)
                {
                    notif.IsRead = true;
                }
            }
            StateHasChanged();
        }
    }

    private async Task ClearAll()
    {
        var success = await Api.ClearAllNotificationsAsync();
        if (success)
        {
            Notifications?.Clear();
            StateHasChanged();
        }
    }

    private async Task LoadDetails(int id)
    {
        IsLoading = true;
        HasError = false;
        StateHasChanged();

        try
        {
            PurchaseRequest = await Api.GetPurchaseRequestByIdAsync(id);
            if (PurchaseRequest == null)
            {
                HasError = true;
                return;
            }

            // Security check for Outlet Manager and Purchase Manager
            if (Auth.IsOutletManager || Auth.IsPurchaseManager)
            {
                if (Auth.OutletID.HasValue && Auth.OutletID.Value > 0 && PurchaseRequest.OutletID != Auth.OutletID.Value)
                {
                    HasError = true;
                    ErrorMessage = "You are not authorized to view purchase requests belonging to another outlet.";
                    PurchaseRequest = null;
                    return;
                }
            }

            var outletsTask = Api.GetOutletsAsync();
            var productsTask = Api.GetProductsAsync();
            var quotationsTask = Api.GetQuotationsAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var orgsTask = Api.GetOrganizationsAsync();
            var notifsTask = Api.GetMyNotificationsAsync();
            var usersTask = Api.GetUsersAsync();

            await Task.WhenAll(outletsTask, productsTask, quotationsTask, vendorsTask, orgsTask, notifsTask, usersTask);

            var outlets = await outletsTask;
            var matchedOutlet = outlets?.FirstOrDefault(o => o.OutletID == PurchaseRequest.OutletID);
            if (matchedOutlet != null)
            {
                OutletName = !string.IsNullOrWhiteSpace(matchedOutlet.OutletName)
                    ? matchedOutlet.OutletName
                    : (!string.IsNullOrWhiteSpace(matchedOutlet.Address) ? $"{matchedOutlet.Address.Split(',')[0].Trim()} Outlet" : $"Outlet #{matchedOutlet.OutletID}");
                OutletAddress = matchedOutlet.Address ?? string.Empty;
            }
            else
            {
                OutletName = !string.IsNullOrWhiteSpace(PurchaseRequest.OutletName) ? PurchaseRequest.OutletName : $"Outlet #{PurchaseRequest.OutletID}";
            }

            var orgs = await orgsTask;
            string resolvedOrgName = string.Empty;

            // 1. Direct from matchedOutlet.OrganizationName
            if (matchedOutlet != null && !string.IsNullOrWhiteSpace(matchedOutlet.OrganizationName) && !matchedOutlet.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
            {
                resolvedOrgName = matchedOutlet.OrganizationName;
            }

            // 2. Lookup in allOrgs by matchedOutlet.OrganizationID
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && matchedOutlet != null && matchedOutlet.OrganizationID > 0)
            {
                var matchedOrg = orgs?.FirstOrDefault(o => o.OrganizationID == matchedOutlet.OrganizationID);
                if (matchedOrg != null && !string.IsNullOrWhiteSpace(matchedOrg.OrganizationName) && !matchedOrg.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
                {
                    resolvedOrgName = matchedOrg.OrganizationName;
                }
            }

            // 3. Direct API call GetOrganizationByIdAsync for matchedOutlet.OrganizationID
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && matchedOutlet != null && matchedOutlet.OrganizationID > 0)
            {
                try
                {
                    var directOrg = await Api.GetOrganizationByIdAsync(matchedOutlet.OrganizationID);
                    if (directOrg != null && !string.IsNullOrWhiteSpace(directOrg.OrganizationName) && !directOrg.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedOrgName = directOrg.OrganizationName;
                    }
                }
                catch { }
            }

            // 4. Sibling outlet matching the same OrganizationID
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && matchedOutlet != null && matchedOutlet.OrganizationID > 0)
            {
                var sibling = outlets?.FirstOrDefault(o => o.OrganizationID == matchedOutlet.OrganizationID && !string.IsNullOrWhiteSpace(o.OrganizationName) && !o.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase));
                if (sibling != null)
                {
                    resolvedOrgName = sibling.OrganizationName;
                }
            }

            // 5. Check invoices for this outlet which carry OrganizationName
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && matchedOutlet != null)
            {
                try
                {
                    var invoices = await Api.GetInvoicesAsync();
                    var invMatch = invoices?.FirstOrDefault(i => i.OutletID == matchedOutlet.OutletID && !string.IsNullOrWhiteSpace(i.OrganizationName) && !i.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase));
                    if (invMatch != null)
                    {
                        resolvedOrgName = invMatch.OrganizationName;
                    }
                }
                catch { }
            }

            // 6. User Auth Organization as fallback if matches
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && Auth.OrganizationID.HasValue && Auth.OrganizationID.Value > 0)
            {
                var authOrg = orgs?.FirstOrDefault(o => o.OrganizationID == Auth.OrganizationID.Value);
                if (authOrg != null && !string.IsNullOrWhiteSpace(authOrg.OrganizationName) && !authOrg.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
                {
                    resolvedOrgName = authOrg.OrganizationName;
                }
                else
                {
                    try
                    {
                        var directAuthOrg = await Api.GetOrganizationByIdAsync(Auth.OrganizationID.Value);
                        if (directAuthOrg != null && !string.IsNullOrWhiteSpace(directAuthOrg.OrganizationName) && !directAuthOrg.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
                        {
                            resolvedOrgName = directAuthOrg.OrganizationName;
                        }
                    }
                    catch { }
                }
            }

            // 7. First named organization in allOrgs
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && orgs != null)
            {
                var firstNamed = orgs.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.OrganizationName) && !o.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase));
                if (firstNamed != null)
                {
                    resolvedOrgName = firstNamed.OrganizationName;
                }
            }

            OrganizationName = !string.IsNullOrWhiteSpace(resolvedOrgName) ? resolvedOrgName : "Organization";

            // PRESERVE ACTUAL HISTORICAL CREATOR
            if (!string.IsNullOrWhiteSpace(PurchaseRequest.CreatedByName))
            {
                CreatedByUserName = PurchaseRequest.CreatedByName;
            }
            else
            {
                var users = await usersTask;
                var creator = users?.FirstOrDefault(u => u.UserID == PurchaseRequest.CreatedByUserID);
                if (creator != null && !string.IsNullOrWhiteSpace(creator.Name))
                {
                    CreatedByUserName = creator.Name;
                }
                else
                {
                    CreatedByUserName = "Organization Staff";
                }
            }

            var products = await productsTask;
            if (products != null)
            {
                ProductNames = products.ToDictionary(p => p.ProductID, p => p.ProductName);
            }

            var quotations = await quotationsTask;
            AssociatedQuotation = quotations?.FirstOrDefault(q => q.RequestID == PurchaseRequest.RequestID);

            if (AssociatedQuotation != null)
            {
                var vendors = await vendorsTask;
                var v = vendors?.FirstOrDefault(ven => ven.VendorID == AssociatedQuotation.VendorID);
                VendorName = v?.VendorName ?? $"Vendor #{AssociatedQuotation.VendorID}";
            }
            else if (!string.IsNullOrWhiteSpace(PurchaseRequest.VendorName))
            {
                VendorName = PurchaseRequest.VendorName;
            }

            Notifications = await notifsTask ?? new List<NotificationDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PurchaseRequestDetails] Error loading details: {ex.Message}");
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private string GetProductName(PurchaseRequestItemDto item)
    {
        if (!string.IsNullOrWhiteSpace(item.ProductName)) return item.ProductName;
        if (ProductNames.TryGetValue(item.ProductID, out var name) && !string.IsNullOrWhiteSpace(name)) return name;
        return $"Product #{item.ProductID}";
    }

    private decimal GetQuotationTotal(QuotationDto q)
    {
        if (q.Items == null || q.Items.Count == 0) return 0;
        return q.Items.Sum(i => i.TotalAmount > 0 ? i.TotalAmount : (i.Quantity * i.UnitPrice + i.TaxAmount));
    }

    private static string GetStatusBadgeClass(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "status-badge-neutral";
        return status.ToLowerInvariant() switch
        {
            "approved" or "accepted" or "completed" or "delivered" or "dispatched" => "status-badge-green",
            "pending" or "submitted" or "created" => "status-badge-green",
            "rejected" or "declined" or "cancelled" => "status-badge-neutral",
            _ => "status-badge-neutral"
        };
    }
}