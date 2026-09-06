using System;
using System.Text.Json;
using Microsoft.JSInterop;
using VenodorManagementFrontend.Models;

namespace VenodorManagementFrontend.Services
{
    public class AuthService
    {
        private const string StorageKey = "svm.auth";
        private readonly IJSRuntime _js;
        private bool _initialized;

        public AuthService(IJSRuntime js)
        {
            _js = js;
        }

        public LoginResponse? CurrentUser { get; private set; }

        public event Action? OnAuthStateChanged;

        public bool IsReady { get; private set; }

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

        public async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var user = JsonSerializer.Deserialize<LoginResponse>(json);
                    if (user != null && !string.IsNullOrWhiteSpace(user.Token))
                    {
                        CurrentUser = user;
                    }
                }

                _initialized = true;
                IsReady = true;
                OnAuthStateChanged?.Invoke();
            }
            catch
            {
                _initialized = true;
                IsReady = true;
                OnAuthStateChanged?.Invoke();
            }
        }

        public void SetUser(LoginResponse user)
        {
            CurrentUser = user;
            _ = PersistAsync();
            OnAuthStateChanged?.Invoke();
        }

        public void Logout()
        {
            CurrentUser = null;
            _ = ClearAsync();
            OnAuthStateChanged?.Invoke();
        }

        private async Task PersistAsync()
        {
            if (CurrentUser == null)
            {
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(CurrentUser);
                await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
            }
            catch
            {
            }
        }

        private async Task ClearAsync()
        {
            try
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            }
            catch
            {
            }
        }
    }
}
