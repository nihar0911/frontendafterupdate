using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.AdminProfile;

public partial class AdminProfile : ComponentBase
{
    private string Name { get; set; } = string.Empty;
    private string Email { get; set; } = string.Empty;
    private string NewPassword { get; set; } = string.Empty;
    private string ConfirmNewPassword { get; set; } = string.Empty;

    private bool ShowNewPassword { get; set; } = false;
    private bool ShowConfirmPassword { get; set; } = false;

    private bool IsSubmitting { get; set; } = false;
    private string? ValidationMessage { get; set; }
    private string? SuccessMessage { get; set; }
    private string? ErrorMessage { get; set; }

    // Sidebar & Profile dropdown state
    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private string ActiveTab { get; set; } = "profile";

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

    private void SetActiveTab(string tab)
    {
        ActiveTab = tab;
    }

    protected override void OnInitialized()
    {
        if (Auth.IsAuthenticated && Auth.CurrentUser != null)
        {
            Name = Auth.CurrentUser.Name ?? Auth.UserName ?? string.Empty;
            Email = Auth.CurrentUser.Email ?? string.Empty;
        }
    }

    private void HandleCancel()
    {
        if (Auth.CurrentUser != null)
        {
            Name = Auth.CurrentUser.Name ?? Auth.UserName ?? string.Empty;
            Email = Auth.CurrentUser.Email ?? string.Empty;
        }
        NewPassword = string.Empty;
        ConfirmNewPassword = string.Empty;
        ValidationMessage = null;
        SuccessMessage = null;
        ErrorMessage = null;
    }

    private async Task HandleSaveProfile()
    {
        ValidationMessage = null;
        SuccessMessage = null;
        ErrorMessage = null;

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

        if (!string.IsNullOrEmpty(NewPassword))
        {
            if (string.IsNullOrWhiteSpace(ConfirmNewPassword))
            {
                ValidationMessage = "Please confirm your new password.";
                return;
            }

            if (NewPassword != ConfirmNewPassword)
            {
                ValidationMessage = "New Password and Confirm New Password do not match.";
                return;
            }
        }

        IsSubmitting = true;
        StateHasChanged();

        try
        {
            var command = new UpdateMyProfileCommand
            {
                Name = Name.Trim(),
                Email = Email.Trim(),
                NewPassword = string.IsNullOrWhiteSpace(NewPassword) ? null : NewPassword
            };

            var result = await Api.UpdateMyProfileAsync(command);
            if (result.Success && result.Data != null)
            {
                Auth.SetUser(result.Data);
                SuccessMessage = "Profile updated successfully.";
                NewPassword = string.Empty;
                ConfirmNewPassword = string.Empty;
            }
            else
            {
                ErrorMessage = !string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? result.ErrorMessage
                    : "Unable to update profile.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AdminProfile] Error updating profile: {ex.Message}");
            ErrorMessage = "An error occurred while updating profile.";
        }
        finally
        {
            IsSubmitting = false;
            StateHasChanged();
        }
    }
}