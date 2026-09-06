using System;
using VenodorManagementFrontend.Models;

namespace VenodorManagementFrontend.Services
{
    public class AuthService
    {
        public LoginResponse? CurrentUser { get; private set; }

        public event Action? OnAuthStateChanged;

        public bool IsAuthenticated => CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.Token);
        public string? Token => CurrentUser?.Token;
        public string Role => CurrentUser?.Role ?? string.Empty;
        public string UserName => CurrentUser?.Name ?? "Guest";
        public string Email => CurrentUser?.Email ?? string.Empty;
        public int UserID => CurrentUser?.UserID ?? 0;
        public int? OrganizationID => CurrentUser?.OrganizationID;
        public int? OutletID => CurrentUser?.OutletID;
        public int? VendorID => CurrentUser?.VendorID;

        public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
        public bool IsOrgManager => string.Equals(Role, "Organization Manager", StringComparison.OrdinalIgnoreCase);
        public bool IsOutletManager => string.Equals(Role, "Outlet Manager", StringComparison.OrdinalIgnoreCase);
        public bool IsVendorManager => string.Equals(Role, "Vendor Manager", StringComparison.OrdinalIgnoreCase);
        public bool IsPurchaseManager => string.Equals(Role, "Purchase Manager", StringComparison.OrdinalIgnoreCase);

        public void SetUser(LoginResponse user)
        {
            CurrentUser = user;
            OnAuthStateChanged?.Invoke();
        }

        public void Logout()
        {
            CurrentUser = null;
            OnAuthStateChanged?.Invoke();
        }
    }
}
