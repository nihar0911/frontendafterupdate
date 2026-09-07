using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.AdminProducts;

public partial class AdminProducts : ComponentBase
{
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;
    private List<ProductDto> Products { get; set; } = new();
    private List<TaxRateDto> TaxRates { get; set; } = new();
    private Dictionary<int, TaxRateDto> TaxRateMap { get; set; } = new();

    private bool IsAddModalOpen { get; set; } = false;
    private ProductDto? EditingProduct { get; set; }
    private ProductDto? SelectedProductDetails { get; set; }
    private bool IsSubmitting { get; set; } = false;
    private string? SuccessMessage { get; set; }
    private string? ErrorMessage { get; set; }

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

    private void OpenDetailsDrawer(ProductDto product)
    {
        SelectedProductDetails = product;
    }

    private void CloseDetailsDrawer()
    {
        SelectedProductDetails = null;
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
            var productsTask = Api.GetProductsAsync();
            var taxRatesTask = Api.GetTaxRatesAsync();

            await Task.WhenAll(productsTask, taxRatesTask);

            Products = await productsTask ?? new List<ProductDto>();
            TaxRates = await taxRatesTask ?? new List<TaxRateDto>();

            TaxRateMap = TaxRates.ToDictionary(t => t.TaxRateID, t => t);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminProducts] Error loading data: {ex.Message}");
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void OpenAddModal()
    {
        IsAddModalOpen = true;
        EditingProduct = null;
        SelectedProductDetails = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private void OpenEditModal(ProductDto product)
    {
        EditingProduct = product;
        IsAddModalOpen = false;
        SelectedProductDetails = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private void CloseForm()
    {
        IsAddModalOpen = false;
        EditingProduct = null;
    }

    private async Task HandleCreateProduct(CreateProductCommand command)
    {
        IsSubmitting = true;
        SuccessMessage = null;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            var result = await Api.CreateProductAsync(command);
            if (result.Success && result.Data != null)
            {
                SuccessMessage = "Product created successfully";
                IsAddModalOpen = false;
                await LoadData();
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to create product. Please verify that the selected Tax Rate exists and is Active.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminProducts] Error creating product: {ex.Message}");
            ErrorMessage = "An error occurred while creating the product.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    private async Task HandleUpdateProduct(UpdateProductCommand command)
    {
        IsSubmitting = true;
        SuccessMessage = null;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            var result = await Api.UpdateProductAsync(command.ProductID, command);
            if (result.Success)
            {
                SuccessMessage = "Product updated successfully";
                EditingProduct = null;
                await LoadData();
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to update product. Please verify that the selected Tax Rate exists and is Active.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminProducts] Error updating product: {ex.Message}");
            ErrorMessage = "An error occurred while updating the product.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    private string GetTaxRateDisplay(int taxRateId)
    {
        if (TaxRateMap.TryGetValue(taxRateId, out var tr))
        {
            return !string.IsNullOrWhiteSpace(tr.TaxName)
                ? $"{tr.TaxName} ({tr.Percentage:F2}%)"
                : $"{tr.Percentage:F2}%";
        }
        return "N/A";
    }

    private TaxRateDto? GetTaxRate(int taxRateId)
    {
        return TaxRateMap.TryGetValue(taxRateId, out var tr) ? tr : null;
    }
}