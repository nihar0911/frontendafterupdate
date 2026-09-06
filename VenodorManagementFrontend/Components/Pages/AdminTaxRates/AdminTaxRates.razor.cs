using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.AdminTaxRates;

public partial class AdminTaxRates : ComponentBase
{
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;
    private List<TaxRateDto> TaxRates { get; set; } = new();

    private bool IsAddModalOpen { get; set; } = false;
    private string AddTaxName { get; set; } = string.Empty;
    private decimal AddPercentage { get; set; } = 0;
    private string AddStatus { get; set; } = "Active";

    private TaxRateDto? EditingTaxRate { get; set; }
    private string EditTaxName { get; set; } = string.Empty;
    private decimal EditPercentage { get; set; } = 0;
    private string EditStatus { get; set; } = "Active";

    private TaxRateDto? SelectedTaxRateDetails { get; set; }

    private bool IsSubmitting { get; set; } = false;
    private string? ValidationMessage { get; set; }
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

    private void OpenDetailsDrawer(TaxRateDto taxRate)
    {
        SelectedTaxRateDetails = taxRate;
    }

    private void CloseDetailsDrawer()
    {
        SelectedTaxRateDetails = null;
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
            await LoadTaxRates();
        }
        else
        {
            IsLoading = false;
        }
    }

    private async Task LoadTaxRates()
    {
        IsLoading = true;
        HasError = false;
        StateHasChanged();

        try
        {
            TaxRates = await Api.GetTaxRatesAsync();
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

    private void OpenAddModal()
    {
        IsAddModalOpen = true;
        EditingTaxRate = null;
        SelectedTaxRateDetails = null;
        AddTaxName = string.Empty;
        AddPercentage = 0;
        AddStatus = "Active";
        ValidationMessage = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private void CloseAddModal()
    {
        IsAddModalOpen = false;
        ValidationMessage = null;
    }

    private async Task HandleCreateTaxRate()
    {
        ValidationMessage = null;
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(AddTaxName))
        {
            ValidationMessage = "Tax Name is required.";
            return;
        }

        if (AddPercentage < 0)
        {
            ValidationMessage = "Tax percentage cannot be negative.";
            return;
        }

        IsSubmitting = true;
        StateHasChanged();

        try
        {
            var command = new CreateTaxRateCommand
            {
                TaxName = AddTaxName.Trim(),
                Percentage = AddPercentage,
                Status = AddStatus
            };

            var created = await Api.CreateTaxRateAsync(command);
            if (created != null)
            {
                SuccessMessage = "✓ Tax rate created successfully";
                IsAddModalOpen = false;
                await LoadTaxRates();
            }
            else
            {
                ErrorMessage = "Unable to create tax rate. Please verify that percentage is not negative.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminTaxRates] Error creating tax rate: {ex.Message}");
            ErrorMessage = "An error occurred while creating the tax rate.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    private void OpenEditModal(TaxRateDto taxRate)
    {
        EditingTaxRate = taxRate;
        IsAddModalOpen = false;
        SelectedTaxRateDetails = null;
        EditTaxName = taxRate.TaxName;
        EditPercentage = taxRate.Percentage;
        EditStatus = taxRate.Status;
        ValidationMessage = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private void CloseEditModal()
    {
        EditingTaxRate = null;
        ValidationMessage = null;
    }

    private async Task HandleUpdateTaxRate()
    {
        if (EditingTaxRate == null) return;

        ValidationMessage = null;
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(EditTaxName))
        {
            ValidationMessage = "Tax Name is required.";
            return;
        }

        if (EditPercentage < 0)
        {
            ValidationMessage = "Tax percentage cannot be negative.";
            return;
        }

        IsSubmitting = true;
        StateHasChanged();

        try
        {
            var command = new UpdateTaxRateCommand
            {
                TaxRateID = EditingTaxRate.TaxRateID,
                TaxName = EditTaxName.Trim(),
                Percentage = EditPercentage,
                Status = EditStatus
            };

            var updated = await Api.UpdateTaxRateAsync(EditingTaxRate.TaxRateID, command);
            if (updated != null)
            {
                SuccessMessage = "✓ Tax rate updated successfully";
                EditingTaxRate = null;
                await LoadTaxRates();
            }
            else
            {
                ErrorMessage = "Unable to update tax rate. Please verify that percentage is not negative.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminTaxRates] Error updating tax rate: {ex.Message}");
            ErrorMessage = "An error occurred while updating the tax rate.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }
}