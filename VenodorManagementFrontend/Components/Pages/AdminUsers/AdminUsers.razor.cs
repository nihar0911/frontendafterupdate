using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.AdminUsers;

public partial class AdminUsers : ComponentBase
{
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;
    private List<UserDto> Users { get; set; } = new();
    private List<OrganizationDto> Organizations { get; set; } = new();
    private List<OutletDto> Outlets { get; set; } = new();
    private List<VendorDto> Vendors { get; set; } = new();
    private Dictionary<int, string> OrgNames { get; set; } = new();
    private Dictionary<int, string> OutletNames { get; set; } = new();
    private Dictionary<int, string> VendorNames { get; set; } = new();

    private bool IsAddModalOpen { get; set; } = false;
    private UserDto? EditingUser { get; set; }
    private UserDto? SelectedUserDetails { get; set; }
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

    private void OpenDetailsDrawer(UserDto user)
    {
        SelectedUserDetails = user;
    }

    private void CloseDetailsDrawer()
    {
        SelectedUserDetails = null;
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
            var usersTask = Api.GetUsersAsync();
            var orgsTask = Api.GetOrganizationsAsync();
            var outletsTask = Api.GetOutletsAsync();
            var vendorsTask = Api.GetVendorsAsync();

            await Task.WhenAll(usersTask, orgsTask, outletsTask, vendorsTask);

            Users = await usersTask ?? new List<UserDto>();
            Organizations = await orgsTask ?? new List<OrganizationDto>();
            Outlets = await outletsTask ?? new List<OutletDto>();
            Vendors = await vendorsTask ?? new List<VendorDto>();

            OrgNames = Organizations.ToDictionary(o => o.OrganizationID, o => o.OrganizationName);
            OutletNames = Outlets.ToDictionary(o => o.OutletID, o => string.IsNullOrWhiteSpace(o.OutletName) ? $"Outlet #{o.OutletID}" : o.OutletName);
            VendorNames = Vendors.ToDictionary(v => v.VendorID, v => v.VendorName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminUsers] Error loading data: {ex.Message}");
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
        EditingUser = null;
        SelectedUserDetails = null;
        IsAddModalOpen = true;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private void StartEditUser(UserDto user)
    {
        IsAddModalOpen = false;
        SelectedUserDetails = null;
        EditingUser = user;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private void CloseForm()
    {
        IsAddModalOpen = false;
        EditingUser = null;
    }

    private async Task HandleCreateUser(CreateUserCommand command)
    {
        IsSubmitting = true;
        SuccessMessage = null;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            var result = await Api.CreateUserAsync(command);
            if (result.Success && result.Data != null)
            {
                SuccessMessage = "User created successfully";
                IsAddModalOpen = false;
                EditingUser = null;
                await LoadData();
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to create user.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminUsers] Error creating user: {ex.Message}");
            ErrorMessage = "An error occurred while creating the user.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    private async Task HandleUpdateUser(UpdateUserCommand command)
    {
        if (EditingUser == null) return;

        IsSubmitting = true;
        SuccessMessage = null;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            var result = await Api.UpdateUserAsync(EditingUser.UserID, command);
            if (result.Success)
            {
                SuccessMessage = "User updated successfully";
                EditingUser = null;
                IsAddModalOpen = false;
                await LoadData();
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to update user.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminUsers] Error updating user: {ex.Message}");
            ErrorMessage = "An error occurred while updating the user.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }

    private string GetDisplayRole(UserDto user)
    {
        if (!string.IsNullOrWhiteSpace(user.RoleName))
        {
            return user.RoleName;
        }

        return user.RoleID switch
        {
            1 => "Admin",
            2 => "Organization Manager",
            3 => "Outlet Manager",
            4 => "Vendor Manager",
            5 => "Purchase Manager",
            7 => "Purchase Manager",
            _ => $"Role #{user.RoleID}"
        };
    }

    private string GetRoleSublabel(UserDto user)
    {
        string role = GetDisplayRole(user);
        return role switch
        {
            "Admin" => "System Administrator",
            "Organization Manager" => "Organization Administrator",
            "Outlet Manager" => "Outlet Administrator",
            "Vendor Manager" => "Vendor Administrator",
            "Purchase Manager" => "Purchase Manager",
            _ => "User"
        };
    }

    private string GetUserInitial(UserDto user)
    {
        if (string.IsNullOrWhiteSpace(user.Name))
            return "U";
        return user.Name.Trim().Substring(0, 1).ToUpperInvariant();
    }

    private string GetOutletParentOrgName(int outletId)
    {
        var outlet = Outlets.FirstOrDefault(o => o.OutletID == outletId);
        if (outlet != null && OrgNames.TryGetValue(outlet.OrganizationID, out var orgName))
        {
            return orgName;
        }
        return string.Empty;
    }
}