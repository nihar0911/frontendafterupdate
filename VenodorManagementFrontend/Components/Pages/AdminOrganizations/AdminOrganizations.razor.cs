using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.AdminOrganizations;

public partial class AdminOrganizations : ComponentBase
{
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;
    private List<OrganizationDto> Organizations { get; set; } = new();

    private bool IsAddModalOpen { get; set; } = false;
    private string AddOrgName { get; set; } = string.Empty;
    private string AddAddress { get; set; } = string.Empty;
    private string AddPhone { get; set; } = string.Empty;
    private string AddEmail { get; set; } = string.Empty;

    private OrganizationDto? EditingOrg { get; set; }
    private string EditOrgName { get; set; } = string.Empty;
    private string EditAddress { get; set; } = string.Empty;
    private string EditPhone { get; set; } = string.Empty;
    private string EditEmail { get; set; } = string.Empty;

    private bool IsSubmitting { get; set; } = false;
    private string? ValidationMessage { get; set; }
    private string? SuccessMessage { get; set; }
    private string? ErrorMessage { get; set; }

    // Sidebar, Profile Dropdown & Details Drawer state
    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;
    private OrganizationDto? SelectedOrgDetails { get; set; }

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ToggleProfileDropdown()
    {
        IsProfileDropdownOpen = !IsProfileDropdownOpen;
    }

    private void OpenDetailsDrawer(OrganizationDto org)
    {
        SelectedOrgDetails = org;
    }

    private void CloseDetailsDrawer()
    {
        SelectedOrgDetails = null;
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
            await LoadOrganizations();
        }
        else
        {
            IsLoading = false;
        }
    }

    private async Task LoadOrganizations()
    {
        IsLoading = true;
        HasError = false;
        StateHasChanged();

        try
        {
            Organizations = await Api.GetOrganizationsAsync();
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
        EditingOrg = null;
        SelectedOrgDetails = null;
        AddOrgName = string.Empty;
        AddAddress = string.Empty;
        AddPhone = string.Empty;
        AddEmail = string.Empty;
        ValidationMessage = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private void CloseAddModal()
    {
        IsAddModalOpen = false;
        ValidationMessage = null;
    }

    private async Task HandleCreateOrganization()
    {
        ValidationMessage = null;
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(AddOrgName))
        {
            ValidationMessage = "Organization Name is required.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(AddEmail) && !AddEmail.Contains("@"))
        {
            ValidationMessage = "Please enter a valid email address.";
            return;
        }

        IsSubmitting = true;
        StateHasChanged();

        try
        {
            var command = new CreateOrganizationCommand
            {
                OrganizationName = AddOrgName.Trim(),
                Address = string.IsNullOrWhiteSpace(AddAddress) ? null : AddAddress.Trim(),
                Phone = string.IsNullOrWhiteSpace(AddPhone) ? null : AddPhone.Trim(),
                Email = string.IsNullOrWhiteSpace(AddEmail) ? null : AddEmail.Trim()
            };

            var result = await Api.CreateOrganizationAsync(command);
            if (result.Success && result.Data != null)
            {
                SuccessMessage = "✓ Organization created successfully";
                IsAddModalOpen = false;
                await LoadOrganizations();
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to create organization.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminOrganizations] Error creating organization: {ex.Message}");
            ErrorMessage = "An error occurred while creating the organization.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    private void OpenEditModal(OrganizationDto org)
    {
        EditingOrg = org;
        IsAddModalOpen = false;
        SelectedOrgDetails = null;
        EditOrgName = org.OrganizationName;
        EditAddress = org.Address ?? string.Empty;
        EditPhone = org.Phone ?? string.Empty;
        EditEmail = org.Email ?? string.Empty;
        ValidationMessage = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private void CloseEditModal()
    {
        EditingOrg = null;
        ValidationMessage = null;
    }

    private async Task HandleUpdateOrganization()
    {
        if (EditingOrg == null) return;

        ValidationMessage = null;
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(EditOrgName))
        {
            ValidationMessage = "Organization Name is required.";
            return;
        }

        if (!string.IsNullOrWhiteSpace(EditEmail) && !EditEmail.Contains("@"))
        {
            ValidationMessage = "Please enter a valid email address.";
            return;
        }

        IsSubmitting = true;
        StateHasChanged();

        try
        {
            var command = new UpdateOrganizationCommand
            {
                OrganizationID = EditingOrg.OrganizationID,
                OrganizationName = EditOrgName.Trim(),
                Address = string.IsNullOrWhiteSpace(EditAddress) ? null : EditAddress.Trim(),
                Phone = string.IsNullOrWhiteSpace(EditPhone) ? null : EditPhone.Trim(),
                Email = string.IsNullOrWhiteSpace(EditEmail) ? null : EditEmail.Trim()
            };

            var result = await Api.UpdateOrganizationAsync(EditingOrg.OrganizationID, command);
            if (result.Success && result.Data != null)
            {
                SuccessMessage = "✓ Organization updated successfully";
                EditingOrg = null;
                await LoadOrganizations();
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to update organization.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminOrganizations] Error updating organization: {ex.Message}");
            ErrorMessage = "An error occurred while updating the organization.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }
}