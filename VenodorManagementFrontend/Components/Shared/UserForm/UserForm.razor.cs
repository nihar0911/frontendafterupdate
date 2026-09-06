using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;

namespace VenodorManagementFrontend.Components.Shared.UserForm;

public partial class UserForm : ComponentBase
{

    [Parameter] public UserDto? EditingUser { get; set; }
    [Parameter] public List<OrganizationDto> Organizations { get; set; } = new();
    [Parameter] public List<OutletDto> Outlets { get; set; } = new();
    [Parameter] public List<VendorDto> Vendors { get; set; } = new();
    [Parameter] public bool IsSubmitting { get; set; }

    [Parameter] public EventCallback<CreateUserCommand> OnCreate { get; set; }
    [Parameter] public EventCallback<UpdateUserCommand> OnUpdate { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private string Name { get; set; } = string.Empty;
    private string Email { get; set; } = string.Empty;
    private string Password { get; set; } = string.Empty;
    private string Role { get; set; } = "Admin";
    private int SelectedOrgId { get; set; }
    private int SelectedOutletId { get; set; }
    private int SelectedVendorId { get; set; }
    private string? ValidationMessage { get; set; }

    private int? _lastLoadedUserId;

    protected override void OnParametersSet()
    {
        ValidationMessage = null;
        if (EditingUser != null && EditingUser.UserID != _lastLoadedUserId)
        {
            _lastLoadedUserId = EditingUser.UserID;
            Name = EditingUser.Name;
            Email = EditingUser.Email;
            Password = string.Empty;
            Role = !string.IsNullOrWhiteSpace(EditingUser.RoleName)
                ? EditingUser.RoleName
                : (EditingUser.RoleID == 2 ? "Organization Manager"
                   : EditingUser.RoleID == 3 ? "Outlet Manager"
                   : EditingUser.RoleID == 4 ? "Vendor Manager"
                   : (EditingUser.RoleID == 7 || EditingUser.RoleID == 5 ? "Purchase Manager" : "Admin"));
            SelectedOrgId = EditingUser.OrganizationID ?? 0;
            SelectedOutletId = EditingUser.OutletID ?? 0;
            SelectedVendorId = EditingUser.VendorID ?? 0;
        }
        else if (EditingUser == null && _lastLoadedUserId != null)
        {
            _lastLoadedUserId = null;
            Name = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            Role = "Admin";
            SelectedOrgId = 0;
            SelectedOutletId = 0;
            SelectedVendorId = 0;
        }
    }

    private async Task HandleSubmit()
    {
        ValidationMessage = null;

        if (string.IsNullOrWhiteSpace(Name))
        {
            ValidationMessage = "Name is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@"))
        {
            ValidationMessage = "Please enter a valid email address.";
            return;
        }

        if (EditingUser == null && string.IsNullOrWhiteSpace(Password))
        {
            ValidationMessage = "Password is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Role))
        {
            ValidationMessage = "Role is required.";
            return;
        }

        int? finalOrgId = null;
        int? finalOutletId = null;
        int? finalVendorId = null;

        if (Role == "Organization Manager")
        {
            if (SelectedOrgId <= 0)
            {
                ValidationMessage = "Please select an Organization.";
                return;
            }
            finalOrgId = SelectedOrgId;
        }
        else if (Role == "Outlet Manager")
        {
            if (SelectedOutletId <= 0)
            {
                ValidationMessage = "Please select an Outlet.";
                return;
            }
            finalOutletId = SelectedOutletId;
        }
        else if (Role == "Vendor Manager")
        {
            if (SelectedVendorId <= 0)
            {
                ValidationMessage = "Please select an Assigned Vendor.";
                return;
            }
            finalVendorId = SelectedVendorId;
        }
        else if (Role == "Purchase Manager")
        {
            if (SelectedOutletId <= 0)
            {
                ValidationMessage = "Please select an Outlet for the Purchase Manager.";
                return;
            }
            var selectedOutlet = Outlets.FirstOrDefault(o => o.OutletID == SelectedOutletId);
            if (selectedOutlet == null)
            {
                ValidationMessage = "The selected outlet could not be found in the system.";
                return;
            }
            finalOutletId = SelectedOutletId;
            finalOrgId = selectedOutlet.OrganizationID;
        }

        int roleId = Role switch
        {
            "Admin" => 1,
            "Organization Manager" => 2,
            "Outlet Manager" => 3,
            "Vendor Manager" => 4,
            "Purchase Manager" => 7,
            _ => 1
        };

        if (EditingUser != null)
        {
            var updateCmd = new UpdateUserCommand
            {
                UserID = EditingUser.UserID,
                Name = Name.Trim(),
                Email = Email.Trim(),
                Password = string.IsNullOrWhiteSpace(Password) ? null : Password,
                Role = Role,
                RoleID = roleId,
                OrganizationID = finalOrgId,
                OutletID = finalOutletId,
                VendorID = finalVendorId
            };
            await OnUpdate.InvokeAsync(updateCmd);
        }
        else
        {
            var createCmd = new CreateUserCommand
            {
                Name = Name.Trim(),
                Email = Email.Trim(),
                Password = Password,
                Role = Role,
                RoleID = roleId,
                OrganizationID = finalOrgId,
                OutletID = finalOutletId,
                VendorID = finalVendorId
            };
            await OnCreate.InvokeAsync(createCmd);
        }
    }

    private async Task HandleCancel()
    {
        await OnCancel.InvokeAsync();
    }

}