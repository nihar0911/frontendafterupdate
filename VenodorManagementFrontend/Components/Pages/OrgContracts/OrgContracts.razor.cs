using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.OrgContracts;

public partial class OrgContracts : ComponentBase
{
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;

    private List<ContractDto> AllOrgContracts { get; set; } = new();
    private List<OutletDto> OrgOutlets { get; set; } = new();
    private string OrganizationName { get; set; } = "Organization";
    private Dictionary<int, VendorDto> _vendorsDict { get; set; } = new();
    private string? SuccessMessage { get; set; }

    private string SearchQuery { get; set; } = string.Empty;
    private int SelectedOutletId { get; set; } = 0;
    private string SelectedSort { get; set; } = "Latest";

    // Sidebar & Profile
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

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login", true);
    }

    private string GetUserInitial()
    {
        if (!string.IsNullOrWhiteSpace(Auth.UserName) && Auth.UserName != "Guest")
        {
            return Auth.UserName[0].ToString().ToUpperInvariant();
        }
        return "O";
    }

    protected override async Task OnInitializedAsync()
    {
        if (Auth.IsAuthenticated)
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
            var contractsTask = Api.GetContractsAsync();
            var outletsTask = Api.GetOutletsAsync();
            var productsTask = Api.GetProductsAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var orgsTask = Api.GetOrganizationsAsync();

            await Task.WhenAll(contractsTask, outletsTask, productsTask, vendorsTask, orgsTask);

            var allContracts = await contractsTask ?? new List<ContractDto>();
            var allOutlets = await outletsTask ?? new List<OutletDto>();
            var allProducts = await productsTask ?? new List<ProductDto>();
            var allVendors = await vendorsTask ?? new List<VendorDto>();
            var allOrgs = await orgsTask ?? new List<OrganizationDto>();

            var productsDict = allProducts.ToDictionary(p => p.ProductID, p => p);
            _vendorsDict = allVendors.ToDictionary(v => v.VendorID, v => v);
            var vendorsDict = _vendorsDict;
            var outletsDict = allOutlets.ToDictionary(o => o.OutletID, o => o);

            int userOrgId = Auth.OrganizationID ?? 0;
            var orgObj = allOrgs.FirstOrDefault(og => og.OrganizationID == userOrgId);
            if (orgObj != null && !string.IsNullOrWhiteSpace(orgObj.OrganizationName))
            {
                OrganizationName = orgObj.OrganizationName;
            }

            // Filter outlets for this organization
            if (userOrgId > 0)
            {
                OrgOutlets = allOutlets.Where(o => o.OrganizationID == userOrgId).ToList();
            }
            else
            {
                OrgOutlets = allOutlets;
            }

            var outletIds = OrgOutlets.Select(o => o.OutletID).ToHashSet();

            // Enrich and filter contracts belonging to this organization's outlets
            var orgContracts = allContracts
                .Where(c => (userOrgId > 0 && c.OrganizationID == userOrgId) || (c.OutletID > 0 && outletIds.Contains(c.OutletID)))
                .ToList();

            foreach (var contract in orgContracts)
            {
                if (outletsDict.TryGetValue(contract.OutletID, out var outl))
                {
                    contract.OutletName = outl.OutletName;
                    contract.OrganizationName = OrganizationName;
                }
                else if (string.IsNullOrWhiteSpace(contract.OutletName))
                {
                    contract.OutletName = $"Outlet #{contract.OutletID}";
                }

                if (productsDict.TryGetValue(contract.ProductID, out var pr))
                {
                    if (string.IsNullOrWhiteSpace(contract.ProductName)) contract.ProductName = pr.ProductName;
                    if (string.IsNullOrWhiteSpace(contract.Unit)) contract.Unit = pr.Unit ?? "Kg";
                }
                else if (string.IsNullOrWhiteSpace(contract.ProductName))
                {
                    contract.ProductName = $"Product #{contract.ProductID}";
                }

                var firstAlloc = contract.Allocations?.FirstOrDefault();
                int vendorId = contract.VendorID ?? firstAlloc?.VendorID ?? 0;
                if (vendorsDict.TryGetValue(vendorId, out var ven))
                {
                    if (string.IsNullOrWhiteSpace(contract.VendorName)) contract.VendorName = ven.VendorName;
                }
                else if (string.IsNullOrWhiteSpace(contract.VendorName))
                {
                    contract.VendorName = $"Vendor #{vendorId}";
                }

                if (string.IsNullOrWhiteSpace(contract.Unit)) contract.Unit = "Kg";
            }

            AllOrgContracts = orgContracts;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrgContracts] Error: {ex.Message}");
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private IEnumerable<ContractDto> FilteredContracts
    {
        get
        {
            var query = AllOrgContracts.AsEnumerable();

            if (SelectedOutletId > 0)
            {
                query = query.Where(c => c.OutletID == SelectedOutletId);
            }

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                string term = SearchQuery.Trim().ToLowerInvariant();
                query = query.Where(c =>
                    c.ContractID.ToString().Contains(term) ||
                    (!string.IsNullOrEmpty(c.ProductName) && c.ProductName.ToLowerInvariant().Contains(term)) ||
                    (!string.IsNullOrEmpty(c.VendorName) && c.VendorName.ToLowerInvariant().Contains(term)) ||
                    (!string.IsNullOrEmpty(c.OutletName) && c.OutletName.ToLowerInvariant().Contains(term))
                );
            }

            return SelectedSort switch
            {
                "Oldest" => query.OrderBy(c => c.ContractID),
                "ValueHigh" => query.OrderByDescending(c => c.TotalAmount > 0 ? c.TotalAmount : (c.TotalQuantity * c.UnitPrice)),
                "Outlet" => query.OrderBy(c => c.OutletName).ThenByDescending(c => c.ContractID),
                _ => query.OrderByDescending(c => c.ContractID)
            };
        }
    }

    private int ActiveContractsCount => AllOrgContracts.Count(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase));
    private int OutletsWithContractsCount => AllOrgContracts.Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase)).Select(c => c.OutletID).Distinct().Count();
    private decimal TotalContractedValue => AllOrgContracts.Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase)).Sum(c => c.TotalAmount > 0 ? c.TotalAmount : (c.TotalQuantity * c.UnitPrice + c.TaxAmount));
    private decimal TotalContractedQuantity => AllOrgContracts.Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase)).Sum(c => c.TotalQuantity);

    // ==========================================
    // RESET / RENEW CONTRACT LOGIC (FOR CAPACITY REACHED)
    // ==========================================
    private ContractDto? ContractToReset { get; set; }
    private bool IsResetModalOpen { get; set; } = false;
    private bool IsResetting { get; set; } = false;
    private string? ResetErrorMessage { get; set; }

    private string EnteredQuantityString { get; set; } = string.Empty;
    private decimal? NewTotalQuantity { get; set; }
    private string? QuantityValidationError { get; set; }
    private List<VendorAllocationPreviewItem> AllocationPreviewItems { get; set; } = new();
    private decimal TotalAllocatedPreview => AllocationPreviewItems.Sum(a => a.NewAllocatedQuantity);

    private bool IsQuantityValid =>
        !string.IsNullOrWhiteSpace(EnteredQuantityString) &&
        string.IsNullOrEmpty(QuantityValidationError) &&
        NewTotalQuantity.HasValue &&
        NewTotalQuantity.Value > 0;

    private async Task PromptResetContract(ContractDto contract)
    {
        ContractToReset = contract;
        ResetErrorMessage = null;
        QuantityValidationError = null;
        EnteredQuantityString = string.Empty;
        NewTotalQuantity = null;
        AllocationPreviewItems.Clear();

        if (contract.Allocations == null || contract.Allocations.Count == 0)
        {
            try
            {
                var fullContract = await Api.GetContractByIdAsync(contract.ContractID);
                if (fullContract?.Allocations != null && fullContract.Allocations.Count > 0)
                {
                    ContractToReset.Allocations = fullContract.Allocations;
                }
            }
            catch { }
        }

        IsResetModalOpen = true;
    }

    private void OnQuantityInput(ChangeEventArgs e)
    {
        EnteredQuantityString = e.Value?.ToString() ?? string.Empty;
        ValidateAndRecalculate();
    }

    private bool ValidateAndRecalculate()
    {
        QuantityValidationError = null;
        AllocationPreviewItems.Clear();

        if (string.IsNullOrWhiteSpace(EnteredQuantityString))
        {
            QuantityValidationError = "New Total Contract Quantity is required.";
            NewTotalQuantity = null;
            return false;
        }

        string trimmed = EnteredQuantityString.Trim();
        if (!decimal.TryParse(trimmed, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedQty) &&
            !decimal.TryParse(trimmed, out parsedQty))
        {
            QuantityValidationError = "Please enter a valid decimal number.";
            NewTotalQuantity = null;
            return false;
        }

        if (parsedQty < 0)
        {
            QuantityValidationError = "Quantity cannot be negative.";
            NewTotalQuantity = null;
            return false;
        }

        if (parsedQty == 0)
        {
            QuantityValidationError = "New Total Contract Quantity must be greater than 0.";
            NewTotalQuantity = null;
            return false;
        }

        NewTotalQuantity = parsedQty;
        CalculateAllocationPreview(parsedQty);
        return true;
    }

    private void CalculateAllocationPreview(decimal newQty)
    {
        AllocationPreviewItems.Clear();
        if (ContractToReset == null || newQty <= 0) return;

        var allocations = ContractToReset.Allocations;
        if (allocations != null && allocations.Count > 0)
        {
            foreach (var alloc in allocations)
            {
                var item = new VendorAllocationPreviewItem
                {
                    VendorID = alloc.VendorID,
                    VendorName = GetVendorName(alloc),
                    AllocationPercentage = alloc.AllocationPercentage,
                    NewAllocatedQuantity = Math.Round(newQty * alloc.AllocationPercentage / 100m, 2, MidpointRounding.AwayFromZero)
                };
                AllocationPreviewItems.Add(item);
            }

            // Ensure the displayed allocation total equals the entered new quantity
            decimal totalAllocated = AllocationPreviewItems.Sum(x => x.NewAllocatedQuantity);
            decimal diff = newQty - totalAllocated;
            if (diff != 0 && AllocationPreviewItems.Count > 0)
            {
                var highestAllocItem = AllocationPreviewItems.OrderByDescending(x => x.AllocationPercentage).First();
                highestAllocItem.NewAllocatedQuantity = Math.Round(highestAllocItem.NewAllocatedQuantity + diff, 2, MidpointRounding.AwayFromZero);
            }
        }
        else
        {
            string vendorName = !string.IsNullOrWhiteSpace(ContractToReset.VendorName)
                ? ContractToReset.VendorName
                : (_vendorsDict.TryGetValue(ContractToReset.VendorID ?? 0, out var v) ? v.VendorName : $"Vendor #{ContractToReset.VendorID ?? 0}");

            AllocationPreviewItems.Add(new VendorAllocationPreviewItem
            {
                VendorID = ContractToReset.VendorID ?? 0,
                VendorName = vendorName,
                AllocationPercentage = 100m,
                NewAllocatedQuantity = newQty
            });
        }
    }

    private string GetVendorName(ContractVendorAllocationDto alloc)
    {
        if (!string.IsNullOrWhiteSpace(alloc.VendorName))
            return alloc.VendorName;
        if (_vendorsDict.TryGetValue(alloc.VendorID, out var v) && !string.IsNullOrWhiteSpace(v.VendorName))
            return v.VendorName;
        return $"Vendor #{alloc.VendorID}";
    }

    private string FormatQuantity(decimal qty)
    {
        return qty % 1 == 0 ? qty.ToString("0") : qty.ToString("N2");
    }

    private string FormatPercentage(decimal pct)
    {
        return pct % 1 == 0 ? $"{pct:0}%" : $"{pct:0.##}%";
    }

    private void CancelReset()
    {
        IsResetModalOpen = false;
        ContractToReset = null;
        ResetErrorMessage = null;
        QuantityValidationError = null;
        EnteredQuantityString = string.Empty;
        NewTotalQuantity = null;
        AllocationPreviewItems.Clear();
        IsResetting = false;
    }

    private async Task ExecuteResetContractAsync()
    {
        if (ContractToReset == null || IsResetting) return;

        if (!ValidateAndRecalculate() || !NewTotalQuantity.HasValue || NewTotalQuantity.Value <= 0)
        {
            return;
        }

        IsResetting = true;
        ResetErrorMessage = null;

        try
        {
            var result = await Api.ResetContractAsync(ContractToReset.ContractID, NewTotalQuantity.Value);
            if (result != null && result.Success)
            {
                int resetContractId = ContractToReset.ContractID;
                decimal renewedQty = NewTotalQuantity.Value;
                string unit = ContractToReset.Unit;

                IsResetModalOpen = false;
                ContractToReset = null;
                EnteredQuantityString = string.Empty;
                NewTotalQuantity = null;
                QuantityValidationError = null;
                AllocationPreviewItems.Clear();

                // Reload contracts to reflect active status and 0 used quantity
                await LoadData();

                SuccessMessage = $"Contract #{resetContractId} renewed successfully with Total Quantity {FormatQuantity(renewedQty)} {unit}. Status is now Active.";
            }
            else
            {
                ResetErrorMessage = result?.ErrorMessage ?? "Failed to reset contract. Please try again.";
            }
        }
        catch (Exception ex)
        {
            ResetErrorMessage = ex.Message;
        }
        finally
        {
            IsResetting = false;
            StateHasChanged();
        }
    }
}

public class VendorAllocationPreviewItem
{
    public int VendorID { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal AllocationPercentage { get; set; }
    public decimal NewAllocatedQuantity { get; set; }
}
