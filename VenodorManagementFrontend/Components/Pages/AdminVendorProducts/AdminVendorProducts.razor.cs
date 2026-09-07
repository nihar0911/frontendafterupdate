using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.AdminVendorProducts;

public partial class AdminVendorProducts : ComponentBase
{
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;

    private List<VendorProductDto> VendorProducts { get; set; } = new();
    private List<VendorDto> Vendors { get; set; } = new();
    private List<ProductDto> Products { get; set; } = new();

    private Dictionary<int, VendorDto> VendorMap { get; set; } = new();
    private Dictionary<int, ProductDto> ProductMap { get; set; } = new();
    private Dictionary<int, string> VendorNameMap { get; set; } = new();
    private Dictionary<int, string> ProductNameMap { get; set; } = new();

    private bool IsFormOpen { get; set; } = false;
    private bool FormSubmitted { get; set; } = false;
    private bool IsSubmitting { get; set; } = false;

    private string? SuccessMessage { get; set; }
    private string? ErrorMessage { get; set; }

    private int FormVendorID { get; set; } = 0;
    private int FormProductID { get; set; } = 0;
    private decimal FormUnitPrice { get; set; } = 0;
    private int FormDeliveryDays { get; set; } = 1;
    private string FormStatus { get; set; } = "Active";

    private VendorProductDto? EditingMapping { get; set; }
    private VendorProductDto? DeletingMapping { get; set; }
    private VendorProductDto? SelectedMappingDetails { get; set; }

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

    private void OpenDetailsDrawer(VendorProductDto vp)
    {
        SelectedMappingDetails = vp;
    }

    private void CloseDetailsDrawer()
    {
        SelectedMappingDetails = null;
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
            await LoadData();
        }
        else
        {
            IsLoading = false;
        }
    }

    private async Task LoadData()
    {
        IsLoading = true;
        HasError = false;
        StateHasChanged();

        try
        {
            var vpTask = Api.GetVendorProductsAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var productsTask = Api.GetProductsAsync();

            await Task.WhenAll(vpTask, vendorsTask, productsTask);

            VendorProducts = await vpTask ?? new List<VendorProductDto>();
            Vendors = await vendorsTask ?? new List<VendorDto>();
            Products = await productsTask ?? new List<ProductDto>();

            VendorMap = Vendors.ToDictionary(v => v.VendorID, v => v);
            ProductMap = Products.ToDictionary(p => p.ProductID, p => p);
            VendorNameMap = Vendors.ToDictionary(v => v.VendorID, v => v.VendorName);
            ProductNameMap = Products.ToDictionary(p => p.ProductID, p => p.ProductName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminVendorProducts] Error loading data: {ex.Message}");
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private string GetVendorName(int vendorId) =>
        VendorNameMap.TryGetValue(vendorId, out var name) ? name : $"Vendor #{vendorId}";

    private string GetProductName(int productId) =>
        ProductNameMap.TryGetValue(productId, out var name) ? name : $"Product #{productId}";

    private VendorDto? GetVendor(int vendorId) =>
        VendorMap.TryGetValue(vendorId, out var v) ? v : null;

    private ProductDto? GetProduct(int productId) =>
        ProductMap.TryGetValue(productId, out var p) ? p : null;

    private bool IsDuplicateMapping() =>
        VendorProducts.Any(vp =>
            vp.VendorID == FormVendorID &&
            vp.ProductID == FormProductID &&
            string.Equals(vp.Status, "Active", StringComparison.OrdinalIgnoreCase) &&
            (EditingMapping == null || vp.VendorProductID != EditingMapping.VendorProductID));

    private bool IsFormValid() =>
        FormVendorID != 0 && FormProductID != 0 && FormUnitPrice >= 0 && FormDeliveryDays >= 1 && !IsDuplicateMapping();

    private void OpenAddForm()
    {
        EditingMapping = null;
        SelectedMappingDetails = null;
        FormVendorID = 0;
        FormProductID = 0;
        FormUnitPrice = 0;
        FormDeliveryDays = 1;
        FormStatus = "Active";
        FormSubmitted = false;
        DeletingMapping = null;
        SuccessMessage = null;
        ErrorMessage = null;
        IsFormOpen = true;
    }

    private void OpenEditForm(VendorProductDto vp)
    {
        EditingMapping = vp;
        SelectedMappingDetails = null;
        FormVendorID = vp.VendorID;
        FormProductID = vp.ProductID;
        FormUnitPrice = vp.UnitPrice;
        FormDeliveryDays = vp.EstimatedDeliveryDays;
        FormStatus = vp.Status;
        FormSubmitted = false;
        DeletingMapping = null;
        SuccessMessage = null;
        ErrorMessage = null;
        IsFormOpen = true;
    }

    private void CloseForm()
    {
        IsFormOpen = false;
        EditingMapping = null;
        FormSubmitted = false;
    }

    private async Task HandleSave()
    {
        FormSubmitted = true;
        StateHasChanged();

        if (!IsFormValid())
            return;

        IsSubmitting = true;
        SuccessMessage = null;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            if (EditingMapping == null)
            {
                var command = new CreateVendorProductCommand
                {
                    VendorID = FormVendorID,
                    ProductID = FormProductID,
                    UnitPrice = FormUnitPrice,
                    EstimatedDeliveryDays = FormDeliveryDays,
                    Status = FormStatus
                };

                var result = await Api.CreateVendorProductAsync(command);
                if (result.Success && result.Data != null)
                {
                    SuccessMessage = $"Mapping created -- {GetVendorName(FormVendorID)} to {GetProductName(FormProductID)}";
                    CloseForm();
                    await LoadData();
                }
                else
                {
                    ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? result.ErrorMessage
                        : "Unable to create vendor product mapping.";
                }
            }
            else
            {
                var command = new UpdateVendorProductCommand
                {
                    VendorProductID = EditingMapping.VendorProductID,
                    VendorID = FormVendorID,
                    ProductID = FormProductID,
                    UnitPrice = FormUnitPrice,
                    EstimatedDeliveryDays = FormDeliveryDays,
                    Status = FormStatus
                };

                var result = await Api.UpdateVendorProductAsync(EditingMapping.VendorProductID, command);
                if (result.Success)
                {
                    SuccessMessage = $"Mapping updated -- {GetVendorName(FormVendorID)} to {GetProductName(FormProductID)}";
                    CloseForm();
                    await LoadData();
                }
                else
                {
                    ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                        ? result.ErrorMessage
                        : "Unable to update vendor product mapping.";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminVendorProducts] Error saving mapping: {ex.Message}");
            ErrorMessage = "An error occurred while saving the mapping.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    private void OpenDeleteConfirm(VendorProductDto vp)
    {
        DeletingMapping = vp;
        SelectedMappingDetails = null;
        IsFormOpen = false;
        EditingMapping = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private async Task ConfirmDelete()
    {
        if (DeletingMapping == null) return;

        IsSubmitting = true;
        StateHasChanged();

        try
        {
            var vendorName = GetVendorName(DeletingMapping.VendorID);
            var productName = GetProductName(DeletingMapping.ProductID);

            var result = await Api.DeleteVendorProductAsync(DeletingMapping.VendorProductID);
            if (result.Success)
            {
                SuccessMessage = $"Mapping deleted -- {vendorName} to {productName}";
                DeletingMapping = null;
                await LoadData();
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to delete vendor product mapping.";
                DeletingMapping = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminVendorProducts] Error deleting mapping: {ex.Message}");
            ErrorMessage = "An error occurred while deleting the mapping.";
            DeletingMapping = null;
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }
}