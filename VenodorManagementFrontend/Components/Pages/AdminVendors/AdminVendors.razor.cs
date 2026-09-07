using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.AdminVendors;

public partial class AdminVendors : ComponentBase
{
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;
    private List<VendorDto> Vendors { get; set; } = new();

    private bool IsAddModalOpen { get; set; } = false;
    private VendorDto? EditingVendor { get; set; }
    private VendorDto? SelectedVendorDetails { get; set; }
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

    private void OpenDetailsDrawer(VendorDto vendor)
    {
        SelectedVendorDetails = vendor;
    }

    private void CloseDetailsDrawer()
    {
        SelectedVendorDetails = null;
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
            await LoadVendors();
        }
        else
        {
            IsLoading = false;
        }
    }

    private async Task LoadVendors()
    {
        IsLoading = true;
        HasError = false;
        StateHasChanged();

        try
        {
            Vendors = await Api.GetVendorsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminVendors] Error loading vendors: {ex.Message}");
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
        EditingVendor = null;
        SelectedVendorDetails = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private void OpenEditModal(VendorDto vendor)
    {
        EditingVendor = vendor;
        IsAddModalOpen = false;
        SelectedVendorDetails = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private void CloseForm()
    {
        IsAddModalOpen = false;
        EditingVendor = null;
    }

    private async Task HandleCreateVendor(CreateVendorCommand command)
    {
        IsSubmitting = true;
        SuccessMessage = null;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            var result = await Api.CreateVendorAsync(command);
            if (result.Success && result.Data != null)
            {
                SuccessMessage = "Vendor created successfully";
                IsAddModalOpen = false;
                await LoadVendors();
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to create vendor.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminVendors] Error creating vendor: {ex.Message}");
            ErrorMessage = "An error occurred while creating the vendor.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    private async Task HandleUpdateVendor(UpdateVendorCommand command)
    {
        IsSubmitting = true;
        SuccessMessage = null;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            var result = await Api.UpdateVendorAsync(command.VendorID, command);
            if (result.Success)
            {
                SuccessMessage = "Vendor updated successfully";
                EditingVendor = null;
                await LoadVendors();
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to update vendor.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminVendors] Error updating vendor: {ex.Message}");
            ErrorMessage = "An error occurred while updating the vendor.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }
}