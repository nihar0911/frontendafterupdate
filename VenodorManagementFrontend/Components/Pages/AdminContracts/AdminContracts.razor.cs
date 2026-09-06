using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.AdminContracts;

public partial class AdminContracts : ComponentBase
{
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;

    private List<ContractDto> Contracts { get; set; } = new();
    private Dictionary<int, string> OutletNames { get; set; } = new();
    private Dictionary<int, string> ProductNames { get; set; } = new();
    private Dictionary<int, string> VendorNames { get; set; } = new();

    // Sidebar & Profile dropdown state
    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ToggleProfileDropdown()
    {
        IsProfileDropdownOpen = !IsProfileDropdownOpen;
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login");
    }

    protected override async Task OnInitializedAsync()
    {
        if (Auth.IsAuthenticated && Auth.IsAdmin)
        {
            await LoadContracts();
        }
        else
        {
            IsLoading = false;
        }
    }

    private async Task LoadContracts()
    {
        IsLoading = true;
        HasError = false;
        StateHasChanged();

        try
        {
            var contractsTask = Api.GetContractsAsync();
            var outletsTask = Api.GetOutletsAsync();
            var productsTask = Api.GetProductsAsync();
            var vendorsTask = Api.GetVendorsAsync();

            await Task.WhenAll(contractsTask, outletsTask, productsTask, vendorsTask);

            Contracts = await contractsTask ?? new List<ContractDto>();

            var outlets = await outletsTask ?? new List<OutletDto>();
            OutletNames = outlets.ToDictionary(
                o => o.OutletID,
                o => string.IsNullOrWhiteSpace(o.OutletName)
                    ? (!string.IsNullOrWhiteSpace(o.Address) ? $"{o.Address.Split(',')[0].Trim()} Outlet" : $"Outlet #{o.OutletID}")
                    : o.OutletName);

            var products = await productsTask ?? new List<ProductDto>();
            ProductNames = products.ToDictionary(p => p.ProductID, p => p.ProductName);

            var vendors = await vendorsTask ?? new List<VendorDto>();
            VendorNames = vendors.ToDictionary(v => v.VendorID, v => v.VendorName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminContracts] Error loading contracts: {ex.Message}");
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private string GetOutletName(int outletId)
    {
        return OutletNames.TryGetValue(outletId, out var name) ? name : $"Outlet #{outletId}";
    }

    private string GetProductName(int productId)
    {
        return ProductNames.TryGetValue(productId, out var name) ? name : $"Product #{productId}";
    }

    private string GetVendorName(int vendorId)
    {
        return VendorNames.TryGetValue(vendorId, out var name) ? name : $"Vendor #{vendorId}";
    }

    private string FormatDate(DateTime dt)
    {
        return dt.ToString("yyyy-MM-dd HH:mm");
    }
}