using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Shared.LoginForm;

public partial class LoginForm : ComponentBase
{
    [Parameter]
    public string RoleTitle { get; set; } = "User Login";

    [Parameter]
    public string RoleBadge { get; set; } = "Role";

    [Parameter]
    public string RoleBadgeClass { get; set; } = "bg-primary";

    [Parameter]
    public string RoleDescription { get; set; } = "Sign in to access your dashboard workspace.";

    [Parameter]
    public string RoleIcon { get; set; } = string.Empty;

    [Parameter]
    public string RoleIconBgClass { get; set; } = "bg-primary";

    [Parameter]
    public string RoleIconColor { get; set; } = "#5F806B";

    [Parameter]
    public string ExpectedRole { get; set; } = string.Empty;

    [Parameter]
    public string PortalDisplayName { get; set; } = string.Empty;

    private string Email { get; set; } = string.Empty;
    private string Password { get; set; } = string.Empty;
    private string ErrorMessage { get; set; } = string.Empty;
    private bool IsLoading { get; set; } = false;

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
                var authenticatedRole = res.Login.Role ?? string.Empty;

                // Validate that the authenticated role matches the selected portal
                if (!string.IsNullOrEmpty(ExpectedRole))
                {
                    bool isMatch = false;

                    if (string.Equals(ExpectedRole, "Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = string.Equals(authenticatedRole, "Admin", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (string.Equals(ExpectedRole, "Organization Manager", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(ExpectedRole, "Organization", StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = string.Equals(authenticatedRole, "Organization Manager", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(authenticatedRole, "Org Manager", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(authenticatedRole, "Organization", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (string.Equals(ExpectedRole, "Outlet Manager", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(ExpectedRole, "Outlet", StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = string.Equals(authenticatedRole, "Outlet Manager", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(authenticatedRole, "Outlet", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (string.Equals(ExpectedRole, "Vendor Manager", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(ExpectedRole, "Vendor", StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = string.Equals(authenticatedRole, "Vendor Manager", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(authenticatedRole, "Vendor", StringComparison.OrdinalIgnoreCase);
                    }
                    else if (string.Equals(ExpectedRole, "Purchase Manager", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(ExpectedRole, "Purchase", StringComparison.OrdinalIgnoreCase))
                    {
                        isMatch = string.Equals(authenticatedRole, "Purchase Manager", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(authenticatedRole, "Purchase", StringComparison.OrdinalIgnoreCase);
                    }

                    if (!isMatch)
                    {
                        string portalName = !string.IsNullOrEmpty(PortalDisplayName) ? PortalDisplayName : RoleBadge;
                        ErrorMessage = $"These credentials are not valid for the {portalName} portal. Please use an {portalName} account.";
                        return;
                    }
                }

                Auth.SetUser(res.Login);
                if (Auth.IsAdmin)
                {
                    Nav.NavigateTo("/admin");
                }
                else if (Auth.IsOrgManager)
                {
                    Nav.NavigateTo("/org-dashboard");
                }
                else if (Auth.IsVendorManager)
                {
                    Nav.NavigateTo("/vendor-dashboard");
                }
                else if (Auth.IsOutletManager)
                {
                    Nav.NavigateTo("/outlet-manager");
                }
                else if (Auth.IsPurchaseManager)
                {
                    Nav.NavigateTo("/purchase-manager");
                }
            }
            else
            {
                ErrorMessage = "Invalid email or password.";
            }
        }
        catch (HttpRequestException)
        {
            ErrorMessage = "Unable to connect to the server.";
        }
        catch (Exception)
        {
            ErrorMessage = "Invalid email or password.";
        }
        finally
        {
            IsLoading = false;
        }
    }


    private void HandleLogout()
    {
        Auth.Logout();
        Email = string.Empty;
        Password = string.Empty;
        ErrorMessage = string.Empty;
    }
}
