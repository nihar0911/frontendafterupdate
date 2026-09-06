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

    private bool ShowNotificationDropdown { get; set; } = false;
    private List<NotificationDto> Notifications { get; set; } = new();
    private int UnreadNotificationCount => Notifications.Count(n => !n.IsRead);

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
                if (!Auth.OutletID.HasValue || Auth.OutletID.Value <= 0 || PurchaseRequest.OutletID != Auth.OutletID.Value)
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
                    : (!string.IsNullOrWhiteSpace(matchedOutlet.Address) ? $"{matchedOutlet.Address} Outlet" : $"Outlet #{matchedOutlet.OutletID}");
                OutletAddress = matchedOutlet.Address ?? string.Empty;
            }
            else
            {
                OutletName = !string.IsNullOrWhiteSpace(PurchaseRequest.OutletName) ? PurchaseRequest.OutletName : $"Outlet #{PurchaseRequest.OutletID}";
            }

            var orgs = await orgsTask;
            var matchedOrg = orgs?.FirstOrDefault(o => o.OrganizationID == (matchedOutlet?.OrganizationID ?? Auth.OrganizationID ?? 0));
            if (matchedOrg != null && !string.IsNullOrWhiteSpace(matchedOrg.OrganizationName))
            {
                OrganizationName = matchedOrg.OrganizationName;
            }

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
                    CreatedByUserName = "Organization Manager";
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
                VendorName = v?.VendorName ?? GetFallbackVendorName(AssociatedQuotation.VendorID);
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

    private string GetFallbackVendorName(int vendorId)
    {
        return $"Vendor #{vendorId}";
    }

    private decimal GetQuotationTotal(QuotationDto q)
    {
        if (q.Items == null || q.Items.Count == 0) return 0;
        return q.Items.Sum(i => i.TotalAmount > 0 ? i.TotalAmount : (i.Quantity * i.UnitPrice + i.TaxAmount));
    }

    private void ToggleNotifications()
    {
        ShowNotificationDropdown = !ShowNotificationDropdown;
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

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login");
    }
}