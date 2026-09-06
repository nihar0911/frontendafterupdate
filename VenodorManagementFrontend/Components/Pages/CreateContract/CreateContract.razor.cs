using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.CreateContract;

public partial class CreateContract : ComponentBase
{
    [Parameter]
    public int? QuotationId { get; set; }

    [SupplyParameterFromQuery(Name = "quotationId")]
    public int? QueryQuotationId { get; set; }

    private bool IsLoading { get; set; } = true;
    private bool IsProcessing { get; set; } = false;
    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private string? ErrorMessage { get; set; }
    private string? ValidationMessage { get; set; }

    private QuotationDto? Quotation { get; set; }
    private PurchaseRequestDto? PurchaseRequest { get; set; }

    private string OrganizationName { get; set; } = "Organization";
    private string OutletName { get; set; } = "Outlet";
    private string ProductName { get; set; } = string.Empty;
    private string ProductCategory { get; set; } = "Produce";
    private string Unit { get; set; } = "Kg";
    private decimal TotalContractQuantity { get; set; }
    private decimal UnitPrice { get; set; }
    private decimal TaxAmount { get; set; }
    private decimal TotalContractValue { get; set; }

    private DateTime StartDate { get; set; } = DateTime.Today;
    private DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
    private string PaymentMethod { get; set; } = "Bank Transfer";

    // ALLOCATION MODELS
    public class AllocationRowModel
    {
        public int VendorID { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public decimal AllocationPercentage { get; set; } = 100m;
        public decimal AllocatedQuantity { get; set; }
        public bool IsWinner { get; set; } = false;
    }

    private List<AllocationRowModel> AllocationRows { get; set; } = new();
    private List<VendorDto> AllEligibleVendors { get; set; } = new();
    private int SelectedVendorToAddId { get; set; } = 0;

    private decimal TotalAllocationPercentage => AllocationRows.Sum(r => r.AllocationPercentage);
    private decimal TotalAllocatedQuantity => AllocationRows.Sum(r => r.AllocatedQuantity);

    private List<VendorDto> UnallocatedEligibleVendors =>
        AllEligibleVendors.Where(v => !AllocationRows.Any(r => r.VendorID == v.VendorID)).ToList();

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
        if (!string.IsNullOrWhiteSpace(Auth.UserName))
        {
            return Auth.UserName[0].ToString().ToUpperInvariant();
        }
        return "G";
    }

    protected override async Task OnInitializedAsync()
    {
        int targetId = QuotationId ?? QueryQuotationId ?? 0;
        if (targetId > 0)
        {
            await LoadData(targetId);
        }
        else
        {
            // Auto-discover the latest accepted quotation to initialize contract creation
            try
            {
                var quotations = await Api.GetQuotationsAsync();
                var allContracts = await Api.GetContractsAsync();
                var contractedQuoteIds = (allContracts ?? new List<ContractDto>())
                    .Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase) && c.QuotationID.HasValue)
                    .Select(c => c.QuotationID!.Value)
                    .ToHashSet();

                var uncontractedAccepted = quotations?
                    .Where(q => string.Equals(q.Status, "Accepted", StringComparison.OrdinalIgnoreCase) && !contractedQuoteIds.Contains(q.QuotationID))
                    .OrderByDescending(q => q.QuotationID)
                    .FirstOrDefault();

                if (uncontractedAccepted != null)
                {
                    await LoadData(uncontractedAccepted.QuotationID);
                }
                else
                {
                    var latestAccepted = quotations?
                        .Where(q => string.Equals(q.Status, "Accepted", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(q => q.QuotationID)
                        .FirstOrDefault();

                    if (latestAccepted != null)
                    {
                        await LoadData(latestAccepted.QuotationID);
                    }
                    else if (quotations != null && quotations.Count > 0)
                    {
                        await LoadData(quotations.OrderByDescending(q => q.QuotationID).First().QuotationID);
                    }
                    else
                    {
                        IsLoading = false;
                        ErrorMessage = "No quotations found to establish a contract. Please accept a quotation first.";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateContract] Init error: {ex.Message}");
                IsLoading = false;
                ErrorMessage = "An error occurred while finding quotations for contract creation.";
            }
        }
    }

    private async Task LoadData(int qId)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            Quotation = await Api.GetQuotationByIdAsync(qId);
            if (Quotation == null)
            {
                ErrorMessage = $"Quotation #{qId} could not be found.";
                return;
            }

            int currentProductId = 0;
            if (Quotation.Items != null && Quotation.Items.Count > 0)
            {
                var qItem = Quotation.Items.First();
                currentProductId = qItem.ProductID;
                UnitPrice = qItem.UnitPrice;
                TaxAmount = qItem.TaxAmount;
                TotalContractQuantity = qItem.Quantity;
                TotalContractValue = Quotation.Items.Sum(i => i.TotalAmount > 0 ? i.TotalAmount : (i.Quantity * i.UnitPrice + i.TaxAmount));

                var products = await Api.GetProductsAsync();
                var p = products?.FirstOrDefault(pr => pr.ProductID == qItem.ProductID);
                if (p != null)
                {
                    ProductName = p.ProductName;
                    ProductCategory = !string.IsNullOrWhiteSpace(p.Category) ? p.Category : "Meat";
                    Unit = !string.IsNullOrWhiteSpace(p.Unit) ? p.Unit : "Kg";
                }
            }

            var requests = await Api.GetPurchaseRequestsAsync();
            PurchaseRequest = requests?.FirstOrDefault(pr => pr.RequestID == Quotation.RequestID);

            var outlets = await Api.GetOutletsAsync();
            int targetOutletId = PurchaseRequest?.OutletID ?? Auth.OutletID ?? 0;
            if (targetOutletId > 0)
            {
                var o = outlets?.FirstOrDefault(outl => outl.OutletID == targetOutletId);
                OutletName = !string.IsNullOrWhiteSpace(o?.OutletName) 
                    ? o.OutletName 
                    : (!string.IsNullOrWhiteSpace(o?.Address) ? $"{o.Address.Split(',')[0].Trim()} Outlet" : $"Outlet #{targetOutletId}");
            }
            else if (outlets != null && outlets.Count > 0)
            {
                OutletName = outlets[0].OutletName;
            }
            else
            {
                OutletName = "Outlet";
            }

            var orgs = await Api.GetOrganizationsAsync();
            int orgId = Auth.OrganizationID ?? 0;
            var org = orgs?.FirstOrDefault(og => og.OrganizationID == orgId);
            OrganizationName = org?.OrganizationName ?? "Organization";

            if (PurchaseRequest != null && PurchaseRequest.Items != null && PurchaseRequest.Items.Count > 0)
            {
                var prItem = PurchaseRequest.Items.First();
                if (TotalContractQuantity == 0) TotalContractQuantity = prItem.Quantity;
                if (string.IsNullOrEmpty(Unit) || Unit == "Units") Unit = prItem.Unit ?? "Kg";
                if (currentProductId == 0) currentProductId = prItem.ProductID;
                if (string.IsNullOrEmpty(ProductName)) ProductName = prItem.ProductName;
            }

            if (Quotation.ValidUntil > StartDate)
            {
                EndDate = Quotation.ValidUntil;
            }

            var allVendors = await Api.GetVendorsAsync();
            var primaryVendor = allVendors?.FirstOrDefault(v => v.VendorID == Quotation.VendorID);
            string primaryVendorName = primaryVendor?.VendorName ?? $"Vendor #{Quotation.VendorID}";

            // Discover eligible vendors dynamically for this product
            var vendorProducts = await Api.GetVendorProductsAsync();
            var activeProductVendorIds = vendorProducts
                .Where(vp => vp.ProductID == currentProductId && string.Equals(vp.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .Select(vp => vp.VendorID)
                .ToHashSet();

            AllEligibleVendors = (allVendors ?? new List<VendorDto>())
                .Where(v => string.Equals(v.Status, "Active", StringComparison.OrdinalIgnoreCase) &&
                           (v.VendorID == Quotation.VendorID || activeProductVendorIds.Contains(v.VendorID)))
                .ToList();

            // Initialize allocation with 100% to accepted quotation winner
            AllocationRows = new List<AllocationRowModel>
            {
                new AllocationRowModel
                {
                    VendorID = Quotation.VendorID,
                    VendorName = primaryVendorName,
                    AllocationPercentage = 100m,
                    AllocatedQuantity = TotalContractQuantity,
                    IsWinner = true
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateContract] LoadData error: {ex.Message}");
            ErrorMessage = "An error occurred while loading contract specifications.";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void OnAllocationPercentageChanged()
    {
        foreach (var row in AllocationRows)
        {
            if (row.AllocationPercentage < 0) row.AllocationPercentage = 0;
            if (row.AllocationPercentage > 100) row.AllocationPercentage = 100;
            row.AllocatedQuantity = Math.Round(TotalContractQuantity * row.AllocationPercentage / 100m, 2, MidpointRounding.AwayFromZero);
        }
        StateHasChanged();
    }

    private void AddSelectedVendor()
    {
        if (SelectedVendorToAddId <= 0) return;
        var vendor = AllEligibleVendors.FirstOrDefault(v => v.VendorID == SelectedVendorToAddId);
        if (vendor == null) return;

        if (!AllocationRows.Any(r => r.VendorID == vendor.VendorID))
        {
            decimal remainingPercent = 100m - TotalAllocationPercentage;
            if (remainingPercent < 0) remainingPercent = 0;

            AllocationRows.Add(new AllocationRowModel
            {
                VendorID = vendor.VendorID,
                VendorName = vendor.VendorName,
                AllocationPercentage = remainingPercent,
                AllocatedQuantity = Math.Round(TotalContractQuantity * remainingPercent / 100m, 2, MidpointRounding.AwayFromZero),
                IsWinner = false
            });

            SelectedVendorToAddId = 0;
            OnAllocationPercentageChanged();
        }
    }

    private void RemoveVendorAllocation(int vendorId)
    {
        if (AllocationRows.Count <= 1) return;
        AllocationRows.RemoveAll(r => r.VendorID == vendorId);
        if (AllocationRows.Count == 1)
        {
            AllocationRows[0].AllocationPercentage = 100m;
        }
        OnAllocationPercentageChanged();
    }

    private async Task SubmitContractCreation()
    {
        if (Quotation == null || Quotation.QuotationID <= 0) return;

        if (TotalAllocationPercentage != 100m)
        {
            ValidationMessage = $"Allocation must total 100%. Current total: {TotalAllocationPercentage:0.##}%.";
            return;
        }

        var positiveAllocations = AllocationRows.Where(r => r.AllocationPercentage > 0).ToList();
        if (positiveAllocations.Count == 0)
        {
            ValidationMessage = "At least one vendor must have a positive allocation percentage.";
            return;
        }

        IsProcessing = true;
        ValidationMessage = null;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            var allocationsPayload = positiveAllocations.Select(r => new CreateContractVendorAllocationDto
            {
                VendorID = r.VendorID,
                AllocationPercentage = r.AllocationPercentage
            }).ToList();

            var contract = await Api.CreateContractFromQuotationAsync(Quotation.QuotationID, allocationsPayload);
            if (contract == null || contract.ContractID <= 0)
            {
                // Direct fallback via CreateContractCommand
                var qItem = Quotation.Items?.FirstOrDefault();
                int prodId = qItem?.ProductID ?? 0;
                decimal qty = qItem?.Quantity ?? TotalContractQuantity;
                int outletId = PurchaseRequest?.OutletID ?? 0;
                if (outletId == 0 && Auth.OutletID.HasValue) outletId = Auth.OutletID.Value;

                var cmd = new CreateContractCommand
                {
                    QuotationID = Quotation.QuotationID,
                    OutletID = outletId,
                    ProductID = prodId,
                    TotalQuantity = qty,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    PaymentMethod = PaymentMethod,
                    Allocations = allocationsPayload
                };
                contract = await Api.CreateContractAsync(cmd);
            }

            if (contract != null && contract.ContractID > 0)
            {
                Nav.NavigateTo($"/contract/{contract.ContractID}");
            }
            else
            {
                ErrorMessage = "Failed to create contract. Ensure allocation percentages total exactly 100% and selected vendors actively supply this product.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateContract] Submit error: {ex.Message}");
            ErrorMessage = "An error occurred while communicating with the server to create the contract.";
        }
        finally
        {
            IsProcessing = false;
            StateHasChanged();
        }
    }

    private void GoBack()
    {
        Nav.NavigateTo("/organization/contracts");
    }
}