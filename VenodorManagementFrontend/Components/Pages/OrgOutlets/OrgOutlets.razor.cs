using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.OrgOutlets;

public partial class OrgOutlets : ComponentBase
{
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;
    private string OrganizationName { get; set; } = string.Empty;
    private string SearchQuery { get; set; } = string.Empty;
    private string SelectedStatusFilter { get; set; } = "All";

    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private List<OutletDto> AllOutlets { get; set; } = new();
    private List<PurchaseRequestDto> PurchaseRequests { get; set; } = new();
    private List<ContractDto> Contracts { get; set; } = new();
    private OutletDto? SelectedOutlet { get; set; }
    private string? ActionMessage { get; set; }

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ToggleProfileDropdown()
    {
        IsProfileDropdownOpen = !IsProfileDropdownOpen;
    }

    private IEnumerable<OutletDto> FilteredOutlets
    {
        get
        {
            var result = AllOutlets.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                result = result.Where(o =>
                    o.OutletName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    (o.Address != null && o.Address.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)) ||
                    o.OutletID.ToString().Contains(SearchQuery));
            }

            return result;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (Auth.IsAuthenticated && (Auth.IsOrgManager || Auth.IsAdmin))
        {
            await LoadData();
        }
        else
        {
            IsLoading = false;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (Auth.IsAuthenticated && (Auth.IsOrgManager || Auth.IsAdmin))
        {
            await LoadData();
        }
    }

    private async Task LoadData()
    {
        IsLoading = true;
        HasError = false;
        StateHasChanged();

        try
        {
            var orgsTask = Api.GetOrganizationsAsync();
            var outletsTask = Api.GetOutletsAsync();
            var requestsTask = Api.GetPurchaseRequestsAsync();
            var contractsTask = Api.GetContractsAsync();

            await Task.WhenAll(orgsTask, outletsTask, requestsTask, contractsTask);

            var orgs = await orgsTask;
            if (Auth.OrganizationID.HasValue && orgs != null)
            {
                var matching = orgs.FirstOrDefault(o => o.OrganizationID == Auth.OrganizationID.Value);
                OrganizationName = matching?.OrganizationName ?? $"Organization #{Auth.OrganizationID.Value}";
            }
            else if (orgs != null && orgs.Count > 0)
            {
                OrganizationName = orgs[0].OrganizationName;
            }

            var outlets = await outletsTask;
            if (outlets != null)
            {
                foreach (var o in outlets)
                {
                    if (string.IsNullOrWhiteSpace(o.OutletName))
                    {
                        o.OutletName = !string.IsNullOrWhiteSpace(o.Address) 
                            ? $"{o.Address.Split(',')[0].Trim()} Outlet" 
                            : $"Outlet #{o.OutletID}";
                    }
                }

                if (Auth.OrganizationID.HasValue && Auth.OrganizationID.Value > 0)
                {
                    AllOutlets = outlets.Where(o => o.OrganizationID == Auth.OrganizationID.Value).ToList();
                }
                else
                {
                    AllOutlets = outlets.ToList();
                }
            }

            var prs = await requestsTask;
            PurchaseRequests = prs ?? new List<PurchaseRequestDto>();

            var contracts = await contractsTask;
            Contracts = contracts ?? new List<ContractDto>();
        }
        catch (Exception)
        {
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private int GetPrCount(int outletId) => PurchaseRequests.Count(r => r.OutletID == outletId);

    private int GetActiveContractCount(int outletId) =>
        Contracts.Count(c => c.OutletID == outletId && string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase));

    private void ViewOutletDetails(OutletDto outlet) => SelectedOutlet = outlet;

    private void CloseModal() => SelectedOutlet = null;

    private async Task HandleApproverRoleChanged(ChangeEventArgs e)
    {
        if (SelectedOutlet == null || !Auth.IsOrgManager)
        {
            return;
        }

        var selected = e.Value?.ToString() ?? "Organization Manager";
        try
        {
            var result = await Api.UpdateOutletAsync(SelectedOutlet.OutletID, new UpdateOutletCommand
            {
                OutletID = SelectedOutlet.OutletID,
                OrganizationID = SelectedOutlet.OrganizationID,
                OutletName = SelectedOutlet.OutletName,
                Address = SelectedOutlet.Address,
                Latitude = SelectedOutlet.Latitude,
                Longitude = SelectedOutlet.Longitude,
                PurchaseOrderApproverRole = selected
            });

            if (result.Success && result.Data != null)
            {
                SelectedOutlet.PurchaseOrderApproverRole = result.Data.PurchaseOrderApproverRole;
                var listed = AllOutlets.FirstOrDefault(o => o.OutletID == SelectedOutlet.OutletID);
                if (listed != null)
                {
                    listed.PurchaseOrderApproverRole = result.Data.PurchaseOrderApproverRole;
                }

                ActionMessage = $"Purchase orders for {SelectedOutlet.OutletName} will be approved by the {result.Data.PurchaseOrderApproverRole}.";
            }
        }
        catch
        {
            ActionMessage = "Unable to update the purchase order approval setting.";
        }
    }

    private string GetUserInitial()
    {
        if (!string.IsNullOrWhiteSpace(Auth.UserName))
        {
            return Auth.UserName.Substring(0, 1).ToUpperInvariant();
        }
        return "O";
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login");
    }
}
