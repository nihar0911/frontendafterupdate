using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.AdminOutlets;

public partial class AdminOutlets : ComponentBase
{
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;
    private List<OutletDto> Outlets { get; set; } = new();
    private List<OrganizationDto> Organizations { get; set; } = new();
    private Dictionary<int, string> OrgNames { get; set; } = new();

    private bool IsAddModalOpen { get; set; } = false;
    private OutletDto? EditingOutlet { get; set; }
    private OutletDto? SelectedOutletDetails { get; set; }
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

    private void OpenDetailsDrawer(OutletDto outlet)
    {
        SelectedOutletDetails = outlet;
    }

    private void CloseDetailsDrawer()
    {
        SelectedOutletDetails = null;
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
            var outletsTask = Api.GetOutletsAsync();
            var orgsTask = Api.GetOrganizationsAsync();

            await Task.WhenAll(outletsTask, orgsTask);

            Outlets = await outletsTask ?? new List<OutletDto>();
            Organizations = await orgsTask ?? new List<OrganizationDto>();

            OrgNames = Organizations.ToDictionary(o => o.OrganizationID, o => o.OrganizationName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminOutlets] Error loading data: {ex.Message}");
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
        EditingOutlet = null;
        SelectedOutletDetails = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private void OpenEditModal(OutletDto outlet)
    {
        EditingOutlet = outlet;
        IsAddModalOpen = false;
        SelectedOutletDetails = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private void CloseForm()
    {
        IsAddModalOpen = false;
        EditingOutlet = null;
    }

    private async Task HandleCreateOutlet(CreateOutletCommand command)
    {
        IsSubmitting = true;
        SuccessMessage = null;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            var result = await Api.CreateOutletAsync(command);
            if (result.Success && result.Data != null)
            {
                SuccessMessage = "Outlet created successfully";
                IsAddModalOpen = false;
                await LoadData();
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to create outlet.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminOutlets] Error creating outlet: {ex.Message}");
            ErrorMessage = "An error occurred while creating the outlet.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    private async Task HandleUpdateOutlet(UpdateOutletCommand command)
    {
        IsSubmitting = true;
        SuccessMessage = null;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            var result = await Api.UpdateOutletAsync(command.OutletID, command);
            if (result.Success)
            {
                SuccessMessage = "Outlet updated successfully";
                EditingOutlet = null;
                await LoadData();
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to update outlet.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminOutlets] Error updating outlet: {ex.Message}");
            ErrorMessage = "An error occurred while updating the outlet.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    private string GetOutletDisplayName(OutletDto outlet)
    {
        if (!string.IsNullOrWhiteSpace(outlet.OutletName))
            return outlet.OutletName;

        if (!string.IsNullOrWhiteSpace(outlet.Address))
            return $"{outlet.Address.Split(',')[0].Trim()} Outlet";

        return $"Outlet #{outlet.OutletID}";
    }

    private string GetOrgName(int orgId)
    {
        return OrgNames.TryGetValue(orgId, out var name) ? name : $"Organization #{orgId}";
    }
}