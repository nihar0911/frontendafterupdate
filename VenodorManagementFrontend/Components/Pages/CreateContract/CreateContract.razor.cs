using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Models.VendorPerformance;
using VenodorManagementFrontend.Helpers;
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
    private bool IsLoadingVendors { get; set; } = false;
    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private string? ErrorMessage { get; set; }
    private string? ValidationMessage { get; set; }

    // Mode determination
    public bool IsStandaloneMode => (QuotationId ?? QueryQuotationId ?? 0) <= 0;

    // Quotation Mode Entities
    private QuotationDto? Quotation { get; set; }
    private PurchaseRequestDto? PurchaseRequest { get; set; }

    // Shared & Standalone Properties
    private string OrganizationName { get; set; } = "Organization";
    private string OutletName { get; set; } = "Outlet";
    private string ProductName { get; set; } = string.Empty;
    private string ProductCategory { get; set; } = "Produce";
    private string Unit { get; set; } = "Kg";
    private decimal TotalContractQuantity { get; set; }
    private decimal UnitPrice { get; set; }
    private decimal TaxAmount { get; set; }
    private decimal TotalContractValue { get; set; }

    // Standalone Specific Selections & Collections
    private int SelectedOutletId { get; set; } = 0;
    private int SelectedProductId { get; set; } = 0;
    private List<OutletDto> OrganizationOutlets { get; set; } = new();
    private List<ProductDto> AvailableProducts { get; set; } = new();

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

    // VENDOR REVIEWS & EVALUATION METRICS
    private Dictionary<int, List<VendorReviewDto>> VendorReviewsDict { get; set; } = new();
    private Dictionary<int, VendorProductDto> VendorProductsDict { get; set; } = new();
    private List<OutletDto> AllSystemOutlets { get; set; } = new();

    // REVIEW MODAL STATE
    private bool IsReviewsModalOpen { get; set; } = false;
    private bool IsLoadingVendorReviews { get; set; } = false;
    private VendorDto? SelectedVendorForReviews { get; set; }
    private List<VendorReviewDto> CurrentVendorReviews { get; set; } = new();

    private decimal CurrentVendorAvgRating => CurrentVendorReviews.Count > 0 ? (decimal)Math.Round(CurrentVendorReviews.Average(r => r.Rating), 1) : 0m;
    private decimal CurrentVendorAvgQuality => CurrentVendorReviews.Count > 0 ? (decimal)Math.Round(CurrentVendorReviews.Average(r => r.ProductQualityRating), 1) : 0m;
    private decimal CurrentVendorAvgDelivery => CurrentVendorReviews.Count > 0 ? (decimal)Math.Round(CurrentVendorReviews.Average(r => r.DeliveryRating), 1) : 0m;
    private VendorReviewDto? LatestReviewInModal => CurrentVendorReviews.FirstOrDefault();

    public class VendorEvalMetrics
    {
        public decimal UnitRate { get; set; }
        public int EstimatedDeliveryDays { get; set; }
        public int ReviewCount { get; set; }
        public decimal AverageRating { get; set; }
        public decimal AverageQualityRating { get; set; }
        public decimal AverageDeliveryRating { get; set; }
        public VendorReviewDto? LatestReview { get; set; }
    }

    private bool CanSubmit => !IsProcessing && TotalAllocationPercentage == 100m &&
        (!IsStandaloneMode || (SelectedOutletId > 0 && SelectedProductId > 0 && TotalContractQuantity > 0 && AllocationRows.Count > 0 && EndDate > StartDate && !string.IsNullOrWhiteSpace(PaymentMethod)));

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
            await LoadQuotationData(targetId);
        }
        else
        {
            await InitializeStandaloneMode();
        }
    }

    private async Task InitializeStandaloneMode()
    {
        IsLoading = true;
        ErrorMessage = null;
        ValidationMessage = null;

        try
        {
            // Load organization details
            var orgs = await Api.GetOrganizationsAsync();
            int orgId = Auth.OrganizationID ?? 0;
            var org = orgs?.FirstOrDefault(og => og.OrganizationID == orgId);
            OrganizationName = org?.OrganizationName ?? "Organization";

            // Load outlets belonging to this organization
            var allOutlets = await Api.GetOutletsAsync();
            AllSystemOutlets = allOutlets ?? new();
            if (orgId > 0)
            {
                OrganizationOutlets = (allOutlets ?? new List<OutletDto>())
                    .Where(o => o.OrganizationID == orgId)
                    .OrderBy(o => o.OutletName)
                    .ToList();
            }
            else
            {
                OrganizationOutlets = (allOutlets ?? new List<OutletDto>())
                    .OrderBy(o => o.OutletName)
                    .ToList();
            }

            // Pre-select user's outlet if valid, otherwise single outlet if only 1 exists
            if (Auth.OutletID.HasValue && OrganizationOutlets.Any(o => o.OutletID == Auth.OutletID.Value))
            {
                SelectedOutletId = Auth.OutletID.Value;
                OutletName = OrganizationOutlets.First(o => o.OutletID == SelectedOutletId).OutletName;
            }
            else if (OrganizationOutlets.Count == 1)
            {
                SelectedOutletId = OrganizationOutlets[0].OutletID;
                OutletName = OrganizationOutlets[0].OutletName;
            }
            else
            {
                SelectedOutletId = 0;
                OutletName = string.Empty;
            }

            // Load active products
            var products = await Api.GetProductsAsync();
            AvailableProducts = (products ?? new List<ProductDto>())
                .Where(p => string.IsNullOrWhiteSpace(p.Status) || string.Equals(p.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.ProductName)
                .ToList();

            SelectedProductId = 0;
            ProductName = string.Empty;
            ProductCategory = string.Empty;
            Unit = "Kg";
            TotalContractQuantity = 0;
            AllocationRows.Clear();
            AllEligibleVendors.Clear();
            VendorReviewsDict.Clear();
            VendorProductsDict.Clear();
            SelectedVendorToAddId = 0;
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddDays(30);
            PaymentMethod = "Bank Transfer";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateContract] Standalone init error: {ex.Message}");
            ErrorMessage = "An error occurred while initializing standalone contract creation.";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task OnOutletSelected()
    {
        var outlet = OrganizationOutlets.FirstOrDefault(o => o.OutletID == SelectedOutletId);
        OutletName = outlet?.OutletName ?? string.Empty;
        await CheckAndLoadEligibleVendors();
    }

    private async Task OnProductSelected()
    {
        var prod = AvailableProducts.FirstOrDefault(p => p.ProductID == SelectedProductId);
        if (prod != null)
        {
            ProductName = prod.ProductName;
            ProductCategory = !string.IsNullOrWhiteSpace(prod.Category) ? prod.Category : "Produce";
            Unit = !string.IsNullOrWhiteSpace(prod.Unit) ? prod.Unit : "Kg";
        }
        else
        {
            ProductName = string.Empty;
            ProductCategory = string.Empty;
            Unit = "Kg";
        }

        // Reset existing vendor allocations when product changes
        AllocationRows.Clear();
        SelectedVendorToAddId = 0;

        await CheckAndLoadEligibleVendors();
    }

    private async Task CheckAndLoadEligibleVendors()
    {
        if (SelectedOutletId > 0 && SelectedProductId > 0)
        {
            await LoadEligibleVendorsAsync();
        }
        else
        {
            AllEligibleVendors.Clear();
            AllocationRows.Clear();
            VendorReviewsDict.Clear();
            VendorProductsDict.Clear();
            SelectedVendorToAddId = 0;
        }
        StateHasChanged();
    }

    private async Task LoadEligibleVendorsAsync()
    {
        try
        {
            IsLoadingVendors = true;
            StateHasChanged();

            var vendorProducts = await Api.GetVendorProductsAsync();
            var activeVP = (vendorProducts ?? new List<VendorProductDto>())
                .Where(vp => vp.ProductID == SelectedProductId && string.Equals(vp.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var activeProductVendorIds = activeVP
                .Select(vp => vp.VendorID)
                .ToHashSet();

            VendorProductsDict = activeVP
                .GroupBy(vp => vp.VendorID)
                .ToDictionary(g => g.Key, g => g.First());

            var allVendors = await Api.GetVendorsAsync();
            AllEligibleVendors = (allVendors ?? new List<VendorDto>())
                .Where(v => string.Equals(v.Status, "Active", StringComparison.OrdinalIgnoreCase) && activeProductVendorIds.Contains(v.VendorID))
                .OrderBy(v => v.VendorName)
                .ToList();

            // Prune any previously allocated vendors that are no longer eligible
            AllocationRows.RemoveAll(r => !AllEligibleVendors.Any(v => v.VendorID == r.VendorID));
            OnAllocationPercentageChanged();

            // Concurrently fetch reviews for all eligible vendors
            await LoadReviewsForEligibleVendorsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateContract] LoadEligibleVendorsAsync error: {ex.Message}");
        }
        finally
        {
            IsLoadingVendors = false;
            StateHasChanged();
        }
    }

    private async Task LoadQuotationData(int qId)
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
                    ProductCategory = !string.IsNullOrWhiteSpace(p.Category) ? p.Category : "Produce";
                    Unit = !string.IsNullOrWhiteSpace(p.Unit) ? p.Unit : "Kg";
                }
            }

            var requests = await Api.GetPurchaseRequestsAsync();
            PurchaseRequest = requests?.FirstOrDefault(pr => pr.RequestID == Quotation.RequestID);

            var outlets = await Api.GetOutletsAsync();
            AllSystemOutlets = outlets ?? new();
            int targetOutletId = PurchaseRequest?.OutletID ?? Auth.OutletID ?? 0;
            if (targetOutletId > 0)
            {
                var o = outlets?.FirstOrDefault(outl => outl.OutletID == targetOutletId);
                OutletName = !string.IsNullOrWhiteSpace(o?.OutletName) 
                    ? o.OutletName 
                    : (!string.IsNullOrWhiteSpace(o?.Address) ? $"{o.Address.Split(',')[0].Trim()} Outlet" : $"Outlet #{targetOutletId}");
                SelectedOutletId = targetOutletId;
            }
            else if (outlets != null && outlets.Count > 0)
            {
                OutletName = outlets[0].OutletName;
                SelectedOutletId = outlets[0].OutletID;
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
            var activeVP = (vendorProducts ?? new List<VendorProductDto>())
                .Where(vp => vp.ProductID == currentProductId && string.Equals(vp.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var activeProductVendorIds = activeVP
                .Select(vp => vp.VendorID)
                .ToHashSet();

            VendorProductsDict = activeVP
                .GroupBy(vp => vp.VendorID)
                .ToDictionary(g => g.Key, g => g.First());

            AllEligibleVendors = (allVendors ?? new List<VendorDto>())
                .Where(v => string.Equals(v.Status, "Active", StringComparison.OrdinalIgnoreCase) &&
                           (v.VendorID == Quotation.VendorID || activeProductVendorIds.Contains(v.VendorID)))
                .ToList();

            SelectedProductId = currentProductId;
            await LoadReviewsForEligibleVendorsAsync();

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
            Console.WriteLine($"[CreateContract] LoadQuotationData error: {ex.Message}");
            ErrorMessage = "An error occurred while loading contract specifications.";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private void OnTotalQuantityChanged()
    {
        if (TotalContractQuantity < 0) TotalContractQuantity = 0;
        OnAllocationPercentageChanged();
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
            decimal defaultPercent;
            if (AllocationRows.Count == 0)
            {
                defaultPercent = 100m;
            }
            else
            {
                decimal remainingPercent = 100m - TotalAllocationPercentage;
                defaultPercent = remainingPercent > 0 ? remainingPercent : 0m;
            }

            AllocationRows.Add(new AllocationRowModel
            {
                VendorID = vendor.VendorID,
                VendorName = vendor.VendorName,
                AllocationPercentage = defaultPercent,
                AllocatedQuantity = Math.Round(TotalContractQuantity * defaultPercent / 100m, 2, MidpointRounding.AwayFromZero),
                IsWinner = false
            });

            SelectedVendorToAddId = 0;
            OnAllocationPercentageChanged();
        }
    }

    private void RemoveVendorAllocation(int vendorId)
    {
        // In quotation mode, prevent removing if 1 row remains or if winner
        if (!IsStandaloneMode && AllocationRows.Count <= 1) return;

        AllocationRows.RemoveAll(r => r.VendorID == vendorId);
        if (!IsStandaloneMode && AllocationRows.Count == 1)
        {
            AllocationRows[0].AllocationPercentage = 100m;
        }
        OnAllocationPercentageChanged();
    }

    private async Task SubmitContractCreation()
    {
        if (IsStandaloneMode)
        {
            if (SelectedOutletId <= 0)
            {
                ValidationMessage = "Please select an outlet.";
                return;
            }

            if (SelectedProductId <= 0)
            {
                ValidationMessage = "Please select a product.";
                return;
            }

            if (TotalContractQuantity <= 0)
            {
                ValidationMessage = "Total contract quantity must be greater than zero.";
                return;
            }

            if (AllocationRows.Count == 0)
            {
                ValidationMessage = "Please add at least one vendor to the contract allocation.";
                return;
            }

            if (TotalAllocationPercentage != 100m)
            {
                ValidationMessage = $"Total allocation must equal exactly 100%. Current total: {TotalAllocationPercentage:0.##}%.";
                return;
            }

            if (EndDate <= StartDate)
            {
                ValidationMessage = "End date must be after the start date.";
                return;
            }

            if (string.IsNullOrWhiteSpace(PaymentMethod))
            {
                ValidationMessage = "Please select a payment method.";
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

                var command = new CreateContractCommand
                {
                    QuotationID = null,
                    OutletID = SelectedOutletId,
                    ProductID = SelectedProductId,
                    TotalQuantity = TotalContractQuantity,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    PaymentMethod = PaymentMethod,
                    Allocations = allocationsPayload
                };

                var contract = await Api.CreateContractAsync(command);
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
                Console.WriteLine($"[CreateContract] Standalone submit error: {ex.Message}");
                ErrorMessage = "An error occurred while communicating with the server to create the contract.";
            }
            finally
            {
                IsProcessing = false;
                StateHasChanged();
            }
        }
        else
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
    }

    private void GoBack()
    {
        Nav.NavigateTo("/organization/contracts");
    }

    // --- VENDOR REVIEWS LOADING & EVALUATION SUPPORT ---
    private async Task LoadReviewsForEligibleVendorsAsync()
    {
        if (SelectedProductId <= 0 || AllEligibleVendors.Count == 0)
        {
            VendorReviewsDict.Clear();
            return;
        }

        try
        {
            var reviewTasks = AllEligibleVendors
                .Select(v => v.VendorID)
                .Distinct()
                .ToDictionary(
                    vId => vId,
                    vId => Api.GetVendorReviewsAsync(vId, SelectedProductId)
                );

            await Task.WhenAll(reviewTasks.Values);

            foreach (var kvp in reviewTasks)
            {
                try
                {
                    VendorReviewsDict[kvp.Key] = (await kvp.Value) ?? new();
                }
                catch
                {
                    VendorReviewsDict[kvp.Key] = new();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateContract] Error fetching vendor reviews: {ex.Message}");
        }
    }

    private VendorEvalMetrics GetVendorMetrics(int vendorId)
    {
        var metrics = new VendorEvalMetrics();
        if (VendorProductsDict.TryGetValue(vendorId, out var vp))
        {
            metrics.UnitRate = vp.UnitPrice;
            metrics.EstimatedDeliveryDays = vp.EstimatedDeliveryDays;
        }
        else if (UnitPrice > 0)
        {
            metrics.UnitRate = UnitPrice;
        }

        if (VendorReviewsDict.TryGetValue(vendorId, out var reviews) && reviews.Count > 0)
        {
            metrics.ReviewCount = reviews.Count;
            metrics.AverageRating = Math.Round(reviews.Average(r => r.Rating), 1);
            metrics.AverageQualityRating = Math.Round(reviews.Average(r => r.ProductQualityRating), 1);
            metrics.AverageDeliveryRating = Math.Round(reviews.Average(r => r.DeliveryRating), 1);
            metrics.LatestReview = reviews.FirstOrDefault();
        }

        return metrics;
    }

    private async Task OpenVendorReviewsModal(VendorDto vendor)
    {
        SelectedVendorForReviews = vendor;
        IsReviewsModalOpen = true;

        if (VendorReviewsDict.TryGetValue(vendor.VendorID, out var cachedReviews) && cachedReviews != null)
        {
            CurrentVendorReviews = cachedReviews;
            IsLoadingVendorReviews = false;
        }
        else
        {
            IsLoadingVendorReviews = true;
            CurrentVendorReviews = new();
            StateHasChanged();
            try
            {
                var reviews = await Api.GetVendorReviewsAsync(vendor.VendorID, SelectedProductId);
                CurrentVendorReviews = reviews ?? new();
                VendorReviewsDict[vendor.VendorID] = CurrentVendorReviews;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateContract] Error loading reviews for vendor {vendor.VendorID}: {ex.Message}");
                CurrentVendorReviews = new();
            }
            finally
            {
                IsLoadingVendorReviews = false;
            }
        }
        StateHasChanged();
    }

    private async Task OpenVendorReviewsModalById(int vendorId)
    {
        var vendor = AllEligibleVendors.FirstOrDefault(v => v.VendorID == vendorId);
        if (vendor != null)
        {
            await OpenVendorReviewsModal(vendor);
        }
    }

    private void CloseVendorReviewsModal()
    {
        IsReviewsModalOpen = false;
        SelectedVendorForReviews = null;
        CurrentVendorReviews.Clear();
    }

    private string ResolveReviewOutletName(VendorReviewDto? rev)
    {
        if (rev == null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(rev.OutletName) && !rev.OutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase))
            return rev.OutletName;

        if (rev.OutletID > 0)
        {
            var matched = AllSystemOutlets.FirstOrDefault(o => o.OutletID == rev.OutletID);
            if (matched != null && !string.IsNullOrWhiteSpace(matched.OutletName))
                return matched.OutletName;
        }

        if (!string.IsNullOrWhiteSpace(OutletName) && !OutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase))
            return OutletName;

        return !string.IsNullOrWhiteSpace(rev.OutletName) ? rev.OutletName : (rev.OutletID > 0 ? $"Outlet #{rev.OutletID}" : string.Empty);
    }

    private string GetQualitativeRatingText(decimal rating)
    {
        if (rating >= 4.6m) return "Excellent";
        if (rating >= 4.0m) return "Very Good";
        if (rating >= 3.0m) return "Good";
        if (rating >= 2.0m) return "Average";
        return "Poor";
    }
}