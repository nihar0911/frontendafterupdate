using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.Admin;

public partial class Admin : ComponentBase
{

private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;

    private int TotalOrganizations { get; set; }
    private int TotalUsers { get; set; }
    private int TotalVendors { get; set; }
    private int TotalOutlets { get; set; }
    private int TotalProducts { get; set; }
    private int TotalTaxRates { get; set; }
    private int ActiveContracts { get; set; }

    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private List<NotificationDto> Notifications { get; set; } = new();
    private int UnreadNotificationCount => Notifications.Count(n => !n.IsRead);
    private bool ShowNotificationDropdown { get; set; } = false;

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

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ToggleProfileDropdown()
    {
        IsProfileDropdownOpen = !IsProfileDropdownOpen;
    }

    protected override async Task OnInitializedAsync()
    {
        if (Auth.IsAuthenticated && Auth.IsAdmin)
        {
            await LoadDashboardData();
        }
        else
        {
            IsLoading = false;
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
            var usersTask = Api.GetUsersAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var outletsTask = Api.GetOutletsAsync();
            var productsTask = Api.GetProductsAsync();
            var taxRatesTask = Api.GetTaxRatesAsync();
            var contractsTask = Api.GetContractsAsync();
            var notifsTask = Api.GetMyNotificationsAsync();

            await Task.WhenAll(orgsTask, usersTask, vendorsTask, outletsTask, productsTask, taxRatesTask, contractsTask, notifsTask);

            var orgs = await orgsTask;
            var users = await usersTask;
            var vendors = await vendorsTask;
            var outlets = await outletsTask;
            var products = await productsTask;
            var taxRates = await taxRatesTask;
            var contracts = await contractsTask;
            Notifications = await notifsTask ?? new List<NotificationDto>();

            TotalOrganizations = orgs?.Count ?? 0;
            TotalUsers = users?.Count ?? 0;
            TotalVendors = vendors?.Count ?? 0;
            TotalOutlets = outlets?.Count ?? 0;
            TotalProducts = products?.Count ?? 0;
            TotalTaxRates = taxRates?.Count ?? 0;
            ActiveContracts = contracts?.Count(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase)) ?? 0;
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

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login");
    }

}