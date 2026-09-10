using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models.VendorRecommendationSettings;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.VendorRecommendationSettings;

public partial class VendorRecommendationSettings : ComponentBase
{
    private VendorRecommendationSettingsDto? Settings { get; set; }
    private bool IsLoadingSettings { get; set; } = false;
    private bool IsSavingSettings { get; set; } = false;
    private string SettingsSuccessMessage { get; set; } = string.Empty;
    private string SettingsErrorMessage { get; set; } = string.Empty;
    private bool IsSidebarCollapsed { get; set; } = false;
    private bool ShowProfileModal { get; set; } = false;
    private bool ShowProfileDropdown { get; set; } = false;

    private decimal TotalWeight => (Settings?.QualityWeight ?? 0) +
                                   (Settings?.DeliveryWeight ?? 0) +
                                   (Settings?.PriceWeight ?? 0) +
                                   (Settings?.ReliabilityWeight ?? 0);

    private bool IsTotalWeightValid => Math.Round(TotalWeight, 2) == 100.00m;

    private string DisplayName => !string.IsNullOrWhiteSpace(Auth.UserName) && Auth.UserName != "Guest" 
        ? Auth.UserName 
        : (Auth.IsPurchaseManager ? "Purchase Manager" : "Manager");

    protected override async Task OnInitializedAsync()
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        IsLoadingSettings = true;
        SettingsSuccessMessage = string.Empty;
        SettingsErrorMessage = string.Empty;
        StateHasChanged();

        try
        {
            var loaded = await Api.GetVendorRecommendationSettingsAsync();
            if (loaded != null)
            {
                Settings = loaded;
            }
            else
            {
                Settings = new VendorRecommendationSettingsDto();
            }
        }
        catch (Exception ex)
        {
            SettingsErrorMessage = $"Failed to load settings: {ex.Message}. Using default values.";
            Settings = new VendorRecommendationSettingsDto();
        }
        finally
        {
            IsLoadingSettings = false;
            StateHasChanged();
        }
    }

    private void ResetToDefaults()
    {
        Settings = new VendorRecommendationSettingsDto
        {
            QualityWeight = 35.0m,
            DeliveryWeight = 25.0m,
            PriceWeight = 25.0m,
            ReliabilityWeight = 15.0m,
            ReliabilityPointsPerReview = 3.0m,
            NeutralScoreForNewVendors = 70.0m,
            BestQualityThreshold = 4.5m,
            FastestDeliveryThreshold = 4.5m,
            HighQualityRationaleThreshold = 4.0m,
            PrioritizeActiveContracts = true
        };
        SettingsSuccessMessage = string.Empty;
        SettingsErrorMessage = string.Empty;
        StateHasChanged();
    }

    private async Task SaveSettingsAsync()
    {
        SettingsSuccessMessage = string.Empty;
        SettingsErrorMessage = string.Empty;

        if (Settings == null) return;

        if (!IsTotalWeightValid)
        {
            SettingsErrorMessage = $"Total weights must sum to exactly 100.00%. Current sum: {TotalWeight:0.##}%.";
            return;
        }

        IsSavingSettings = true;
        StateHasChanged();

        try
        {
            var (success, message, updated) = await Api.UpdateVendorRecommendationSettingsAsync(Settings);
            if (success)
            {
                SettingsSuccessMessage = "Vendor recommendation settings saved successfully!";
                if (updated != null) Settings = updated;
            }
            else
            {
                SettingsErrorMessage = message;
            }
        }
        catch (Exception ex)
        {
            SettingsErrorMessage = $"An error occurred: {ex.Message}";
        }
        finally
        {
            IsSavingSettings = false;
            StateHasChanged();
        }
    }

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ToggleProfileDropdown()
    {
        ShowProfileDropdown = !ShowProfileDropdown;
    }

    private void OpenProfileModal()
    {
        ShowProfileModal = true;
        ShowProfileDropdown = false;
    }

    private void CloseProfileModal()
    {
        ShowProfileModal = false;
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login", true);
    }
}
