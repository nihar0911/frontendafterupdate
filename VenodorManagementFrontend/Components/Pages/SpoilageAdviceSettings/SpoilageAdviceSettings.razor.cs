using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models.SpoilageAdviceSettings;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.SpoilageAdviceSettings;

public partial class SpoilageAdviceSettings : ComponentBase
{
    private SpoilageAdviceSettingsDto? SpoilageSettings { get; set; }
    private bool IsLoadingSettings { get; set; } = false;
    private bool IsSavingSettings { get; set; } = false;
    private string SettingsSuccessMessage { get; set; } = string.Empty;
    private string SettingsErrorMessage { get; set; } = string.Empty;
    private string DisplayName => !string.IsNullOrWhiteSpace(Auth.UserName) && Auth.UserName != "Guest" ? Auth.UserName : "Vendor Manager";

    protected override async Task OnInitializedAsync()
    {
        await LoadSpoilageSettingsAsync();
    }

    private async Task LoadSpoilageSettingsAsync()
    {
        IsLoadingSettings = true;
        SettingsSuccessMessage = string.Empty;
        SettingsErrorMessage = string.Empty;
        StateHasChanged();

        try
        {
            var loaded = await Api.GetSpoilageAdviceSettingsAsync();
            if (loaded != null)
            {
                SpoilageSettings = loaded;
            }
            else
            {
                SpoilageSettings = new SpoilageAdviceSettingsDto();
            }
        }
        catch (Exception)
        {
            SettingsErrorMessage = "Failed to load settings. Using default values.";
            SpoilageSettings = new SpoilageAdviceSettingsDto();
        }
        finally
        {
            IsLoadingSettings = false;
            StateHasChanged();
        }
    }

    private async Task SaveSpoilageSettingsAsync()
    {
        SettingsSuccessMessage = string.Empty;
        SettingsErrorMessage = string.Empty;

        if (SpoilageSettings == null) return;

        // Validation matching business rules
        if (SpoilageSettings.RecentDeliveriesCount <= 0)
        {
            SettingsErrorMessage = "Recent deliveries must be greater than 0.";
            return;
        }

        if (SpoilageSettings.TrendTolerancePercentage < 0)
        {
            SettingsErrorMessage = "Trend tolerance percentage must be 0 or greater.";
            return;
        }

        if (SpoilageSettings.LowWeightedSpoilageThreshold < 0 ||
            SpoilageSettings.MediumWeightedSpoilageThreshold < 0 ||
            SpoilageSettings.HighWeightedSpoilageThreshold < 0)
        {
            SettingsErrorMessage = "Threshold must be 0 or greater.";
            return;
        }

        if (SpoilageSettings.LowWeightedSpoilageThreshold > SpoilageSettings.MediumWeightedSpoilageThreshold)
        {
            SettingsErrorMessage = "Low threshold cannot be greater than the medium threshold.";
            return;
        }

        if (SpoilageSettings.MediumWeightedSpoilageThreshold > SpoilageSettings.HighWeightedSpoilageThreshold)
        {
            SettingsErrorMessage = "Medium threshold cannot be greater than the high threshold.";
            return;
        }

        if (SpoilageSettings.HighRecentSpoilageThreshold < 0 || SpoilageSettings.HighMaximumSpoilageThreshold < 0)
        {
            SettingsErrorMessage = "Threshold must be 0 or greater.";
            return;
        }

        IsSavingSettings = true;
        StateHasChanged();

        try
        {
            var result = await Api.UpdateSpoilageAdviceSettingsAsync(SpoilageSettings);
            if (result.Success)
            {
                SettingsSuccessMessage = "Spoilage advice settings saved successfully.";
                if (result.Settings != null)
                {
                    SpoilageSettings = result.Settings;
                }
            }
            else
            {
                SettingsErrorMessage = !string.IsNullOrWhiteSpace(result.Message) ? result.Message : "Failed to save settings.";
            }
        }
        catch (Exception ex)
        {
            SettingsErrorMessage = ex.Message;
        }
        finally
        {
            IsSavingSettings = false;
            StateHasChanged();
        }
    }

    private void ResetSpoilageSettingsToDefaults()
    {
        SettingsSuccessMessage = string.Empty;
        SettingsErrorMessage = string.Empty;
        if (SpoilageSettings != null)
        {
            SpoilageSettings.RecentDeliveriesCount = 5;
            SpoilageSettings.TrendTolerancePercentage = 1.0m;
            SpoilageSettings.HighWeightedSpoilageThreshold = 5.0m;
            SpoilageSettings.HighRecentSpoilageThreshold = 6.0m;
            SpoilageSettings.HighMaximumSpoilageThreshold = 10.0m;
            SpoilageSettings.MediumWeightedSpoilageThreshold = 2.0m;
            SpoilageSettings.LowWeightedSpoilageThreshold = 2.0m;
        }
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login");
    }
}
