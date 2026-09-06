using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.Home;

public partial class Home : ComponentBase
{
    private string Email { get; set; } = string.Empty;
    private string Password { get; set; } = string.Empty;
    private string ErrorMessage { get; set; } = string.Empty;
    private bool IsLoading { get; set; } = false;
    private bool ShowPassword { get; set; } = false;

    private void ToggleShowPassword()
    {
        ShowPassword = !ShowPassword;
    }

    private async Task HandleLogin()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter both Email and Password.";
            return;
        }

        IsLoading = true;
        try
        {
            var res = await Api.LoginAsync(Email, Password);
            if (res != null && res.Login != null)
            {
                Auth.SetUser(res.Login);
                if (Auth.IsAdmin)
                    Nav.NavigateTo("/admin");
                else if (Auth.IsOrgManager)
                    Nav.NavigateTo("/org-dashboard");
                else if (Auth.IsVendorManager)
                    Nav.NavigateTo("/vendor-dashboard");
                else if (Auth.IsOutletManager)
                    Nav.NavigateTo("/outlet-manager");
                else if (Auth.IsPurchaseManager)
                    Nav.NavigateTo("/purchase-manager");
                else
                    ErrorMessage = "Login successful but your role is not recognized. Please contact the administrator.";
            }
            else
            {
                ErrorMessage = "Invalid credentials. Please check your email and password and try again.";
            }
        }
        catch (HttpRequestException ex)
        {
            ErrorMessage = ex.Message.Contains("401") || ex.Message.Contains("Unauthorized")
                ? "Invalid credentials. Please verify your email and password."
                : "Unable to connect to the authentication server. Please ensure the backend API is running.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login", true);
    }
}