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
    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private string? ErrorMessage { get; set; }
    private string? ValidationMessage { get; set; }
    private string? SuccessMessage { get; set; }

    // Mode determination
    public bool IsStandaloneMode => (QuotationId ?? QueryQuotationId ?? 0) <= 0;

    // Quotation Mode Entities
    private QuotationDto? Quotation { get; set; }
    private PurchaseRequestDto? PurchaseRequest { get; set; }

    // Specifications & Schedule
    private string OrganizationName { get; set; } = "Organization";
    private string OutletName { get; set; } = "Outlet";
    private int SelectedOutletId { get; set; } = 0;
    private List<OutletDto> OrganizationOutlets { get; set; } = new();
    private List<ProductDto> AvailableProducts { get; set; } = new();
    private List<OutletDto> AllSystemOutlets { get; set; } = new();

    private DateTime StartDate { get; set; } = DateTime.Today;
    private DateTime EndDate { get; set; } = DateTime.Today.AddDays(30);
    private string PaymentMethod { get; set; } = "Bank Transfer";

    // Caches for fast multi-product candidate discovery
    private List<VendorProductDto> _cachedVendorProducts = new();
    private List<VendorDto> _cachedAllVendors = new();

    // =========================================================================
    // MULTI-PRODUCT LINE ITEM MODEL
    // =========================================================================
    // MULTI-PRODUCT LINE ITEM MODEL
    // =========================================================================
    public class ProductContractLine
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = "Produce";
        public string Unit { get; set; } = "Kg";
        public decimal ContractQuantity { get; set; }
        public int SelectedVendorID { get; set; } = 0;
        public string SelectedVendorName { get; set; } = string.Empty;
        public List<VendorRecommendationDto> EligibleVendors { get; set; } = new();
        public bool IsLoadingVendors { get; set; } = false;
        public bool HasActiveContracts { get; set; } = false;
        public string SortBy { get; set; } = "Overall Reviews";
        public Dictionary<int, VendorEvalMetrics> VendorMetrics { get; set; } = new();

        public IEnumerable<VendorRecommendationDto> GetSortedVendors()
        {
            if (EligibleVendors == null || EligibleVendors.Count == 0)
                return Enumerable.Empty<VendorRecommendationDto>();

            if (string.Equals(SortBy, "Unit Price", StringComparison.OrdinalIgnoreCase))
            {
                return EligibleVendors
                    .OrderBy(v => v.UnitPrice > 0 ? v.UnitPrice : decimal.MaxValue)
                    .ThenBy(v => v.VendorName, StringComparer.OrdinalIgnoreCase);
            }
            else // Default: "Overall Reviews" (AverageRating DESC -> TotalFeedbackCount DESC -> VendorName ASC)
            {
                return EligibleVendors
                    .OrderByDescending(v => v.AverageRating)
                    .ThenByDescending(v => v.TotalFeedbackCount)
                    .ThenBy(v => v.VendorName, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public class VendorContractGroup
    {
        public int VendorID { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public List<ProductContractLine> Products { get; set; } = new();
        public decimal TotalQuantity => Products.Sum(p => p.ContractQuantity);
    }

    private List<ProductContractLine> ProductLines { get; set; } = new();

    // New product line entry fields
    private int NewProductId { get; set; } = 0;
    private decimal NewProductQuantity { get; set; } = 0;
    private string NewProductUnit =>
        AvailableProducts.FirstOrDefault(p => p.ProductID == NewProductId)?.Unit ?? "Kg";

    // Computed grouping preview
    public List<VendorContractGroup> GroupedContracts => ProductLines
        .Where(p => p.SelectedVendorID > 0)
        .GroupBy(p => p.SelectedVendorID)
        .Select(g => new VendorContractGroup
        {
            VendorID = g.Key,
            VendorName = g.First().EligibleVendors.FirstOrDefault(v => v.VendorID == g.Key)?.VendorName
                         ?? (g.First().SelectedVendorName.Length > 0 ? g.First().SelectedVendorName : $"Vendor #{g.Key}"),
            Products = g.ToList()
        })
        .ToList();

    public int UnassignedProductsCount => ProductLines.Count(p => p.SelectedVendorID <= 0);

    // Products available for addition (exclude already added products)
    public List<ProductDto> SelectableProducts => AvailableProducts
        .Where(p => !ProductLines.Any(pl => pl.ProductID == p.ProductID))
        .OrderBy(p => p.ProductName)
        .ToList();

    // =========================================================================
    // VENDOR REVIEWS & EVALUATION METRICS
    // =========================================================================
    private Dictionary<int, List<VendorReviewDto>> VendorReviewsDict { get; set; } = new();

    // Review Modal State
    private bool IsReviewsModalOpen { get; set; } = false;
    private bool IsLoadingVendorReviews { get; set; } = false;
    private VendorRecommendationDto? SelectedVendorForReviews { get; set; }
    private string SelectedProductForReviewsName { get; set; } = string.Empty;
    private int SelectedProductForReviewsId { get; set; } = 0;
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

    // =========================================================================
    // VALIDATION & SUBMISSION CRITERIA
    // =========================================================================
    private bool CanSubmit =>
        !IsProcessing &&
        SelectedOutletId > 0 &&
        ProductLines.Count > 0 &&
        ProductLines.All(p => p.ContractQuantity > 0 && p.SelectedVendorID > 0) &&
        EndDate > StartDate &&
        !string.IsNullOrWhiteSpace(PaymentMethod);

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
            // 1. Load organization details
            var orgs = await Api.GetOrganizationsAsync();
            int orgId = Auth.OrganizationID ?? 0;
            var org = orgs?.FirstOrDefault(og => og.OrganizationID == orgId);
            OrganizationName = org?.OrganizationName ?? "Organization";

            // 2. Load outlets belonging to this organization
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

            // 3. Load active products
            var products = await Api.GetProductsAsync();
            AvailableProducts = (products ?? new List<ProductDto>())
                .Where(p => string.IsNullOrWhiteSpace(p.Status) || string.Equals(p.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.ProductName)
                .ToList();

            // 4. Pre-cache vendor product relationships and vendors
            _cachedVendorProducts = await Api.GetVendorProductsAsync() ?? new();
            _cachedAllVendors = await Api.GetVendorsAsync() ?? new();

            ProductLines.Clear();
            NewProductId = 0;
            NewProductQuantity = 0;
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

        // Re-evaluate eligible vendors for all added product lines
        if (ProductLines.Count > 0)
        {
            foreach (var line in ProductLines)
            {
                await LoadEligibleVendorsForProductLineAsync(line);
            }
        }
        StateHasChanged();
    }

    // =========================================================================
    // MULTI-PRODUCT LINE ACTIONS
    // =========================================================================
    private async Task AddProductLine()
    {
        ValidationMessage = null;

        if (NewProductId <= 0)
        {
            ValidationMessage = "Please choose a product to add.";
            return;
        }

        if (NewProductQuantity <= 0)
        {
            ValidationMessage = "Planning quantity must be greater than zero.";
            return;
        }

        var prod = AvailableProducts.FirstOrDefault(p => p.ProductID == NewProductId);
        if (prod == null) return;

        if (ProductLines.Any(p => p.ProductID == prod.ProductID))
        {
            ValidationMessage = $"{prod.ProductName} has already been added to the contract preparation list.";
            return;
        }

        var line = new ProductContractLine
        {
            ProductID = prod.ProductID,
            ProductName = prod.ProductName,
            ProductCategory = !string.IsNullOrWhiteSpace(prod.Category) ? prod.Category : "Produce",
            Unit = !string.IsNullOrWhiteSpace(prod.Unit) ? prod.Unit : "Kg",
            ContractQuantity = NewProductQuantity,
            SelectedVendorID = 0
        };

        ProductLines.Add(line);

        // Reset input fields
        NewProductId = 0;
        NewProductQuantity = 0;

        // Discover eligible vendors for this specific product
        await LoadEligibleVendorsForProductLineAsync(line);
    }

    private void RemoveProductLine(int productId)
    {
        ProductLines.RemoveAll(p => p.ProductID == productId);
        ValidationMessage = null;
        StateHasChanged();
    }

    private void OnProductQuantityChanged(ProductContractLine line, decimal newQuantity)
    {
        if (newQuantity < 0) newQuantity = 0;
        line.ContractQuantity = newQuantity;
        StateHasChanged();
    }

    private void SelectVendorForProductLine(ProductContractLine line, VendorRecommendationDto vendor)
    {
        if (line.SelectedVendorID == vendor.VendorID)
        {
            line.SelectedVendorID = 0;
            line.SelectedVendorName = string.Empty;
        }
        else
        {
            line.SelectedVendorID = vendor.VendorID;
            line.SelectedVendorName = vendor.VendorName;
        }
        StateHasChanged();
    }

    private void OnSortByChanged(ProductContractLine line, string? newSort)
    {
        line.SortBy = string.IsNullOrWhiteSpace(newSort) ? "Overall Reviews" : newSort;
        StateHasChanged();
    }

    private void OnProductVendorChanged(ProductContractLine line, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int vId))
        {
            line.SelectedVendorID = vId;
            var vendor = line.EligibleVendors.FirstOrDefault(v => v.VendorID == vId);
            line.SelectedVendorName = vendor?.VendorName ?? string.Empty;
        }
        else
        {
            line.SelectedVendorID = 0;
            line.SelectedVendorName = string.Empty;
        }
        StateHasChanged();
    }

    private async Task LoadEligibleVendorsForProductLineAsync(ProductContractLine line)
    {
        line.IsLoadingVendors = true;
        StateHasChanged();

        try
        {
            List<VendorRecommendationDto> vendors = new();
            bool hasActiveContracts = false;

            if (SelectedOutletId > 0)
            {
                var vendorResponse = await Api.GetContractEligibleVendorsAsync(line.ProductID, SelectedOutletId);
                if (vendorResponse != null)
                {
                    hasActiveContracts = vendorResponse.HasActiveContracts;
                    vendors = vendorResponse.Vendors ?? new();
                }
            }

            // Fallback if no recommendation endpoint results or SelectedOutletId == 0
            if (vendors.Count == 0 && SelectedOutletId <= 0)
            {
                if (_cachedVendorProducts.Count == 0)
                {
                    _cachedVendorProducts = await Api.GetVendorProductsAsync() ?? new();
                }
                if (_cachedAllVendors.Count == 0)
                {
                    _cachedAllVendors = await Api.GetVendorsAsync() ?? new();
                }

                var activeVP = _cachedVendorProducts
                    .Where(vp => vp.ProductID == line.ProductID && string.Equals(vp.Status, "Active", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var activeVendorIds = activeVP.Select(vp => vp.VendorID).ToHashSet();

                var matchedVendors = _cachedAllVendors
                    .Where(v => string.Equals(v.Status, "Active", StringComparison.OrdinalIgnoreCase) && activeVendorIds.Contains(v.VendorID))
                    .OrderBy(v => v.VendorName)
                    .ToList();

                vendors = matchedVendors.Select(v =>
                {
                    var vp = activeVP.FirstOrDefault(p => p.VendorID == v.VendorID);
                    return new VendorRecommendationDto
                    {
                        VendorID = v.VendorID,
                        VendorName = v.VendorName,
                        ProductID = line.ProductID,
                        ProductName = line.ProductName,
                        UnitPrice = vp?.UnitPrice ?? 0m,
                        EstimatedDeliveryDays = vp?.EstimatedDeliveryDays ?? 0,
                        AverageRating = 0m,
                        TotalFeedbackCount = 0
                    };
                }).ToList();
            }

            line.EligibleVendors = vendors;
            line.HasActiveContracts = hasActiveContracts;

            line.VendorMetrics.Clear();
            foreach (var v in vendors)
            {
                line.VendorMetrics[v.VendorID] = new VendorEvalMetrics
                {
                    UnitRate = v.UnitPrice,
                    EstimatedDeliveryDays = v.EstimatedDeliveryDays,
                    ReviewCount = v.TotalFeedbackCount,
                    AverageRating = v.AverageRating,
                    AverageQualityRating = v.AverageQualityRating,
                    AverageDeliveryRating = v.AverageDeliveryRating
                };
            }

            // Auto-select if exactly 1 eligible vendor exists
            if (line.EligibleVendors.Count == 1)
            {
                line.SelectedVendorID = line.EligibleVendors[0].VendorID;
                line.SelectedVendorName = line.EligibleVendors[0].VendorName;
            }
            else if (line.SelectedVendorID > 0 && !line.EligibleVendors.Any(v => v.VendorID == line.SelectedVendorID))
            {
                // Previously selected vendor is no longer eligible (e.g. outlet changed)
                line.SelectedVendorID = 0;
                line.SelectedVendorName = string.Empty;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateContract] Error loading vendors for product {line.ProductID}: {ex.Message}");
        }
        finally
        {
            line.IsLoadingVendors = false;
            StateHasChanged();
        }
    }

    // =========================================================================
    // QUOTATION MODE HANDLING
    // =========================================================================
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

            if (Quotation.ValidUntil > StartDate)
            {
                EndDate = Quotation.ValidUntil;
            }

            // Pre-cache vendors and vendor products
            _cachedVendorProducts = await Api.GetVendorProductsAsync() ?? new();
            _cachedAllVendors = await Api.GetVendorsAsync() ?? new();

            var products = await Api.GetProductsAsync();
            AvailableProducts = products ?? new();

            var primaryVendor = _cachedAllVendors.FirstOrDefault(v => v.VendorID == Quotation.VendorID);
            string primaryVendorName = primaryVendor?.VendorName ?? $"Vendor #{Quotation.VendorID}";

            ProductLines.Clear();

            // Populate product lines from quotation items
            if (Quotation.Items != null && Quotation.Items.Count > 0)
            {
                foreach (var qItem in Quotation.Items)
                {
                    var p = AvailableProducts.FirstOrDefault(pr => pr.ProductID == qItem.ProductID);
                    var line = new ProductContractLine
                    {
                        ProductID = qItem.ProductID,
                        ProductName = p?.ProductName ?? $"Product #{qItem.ProductID}",
                        ProductCategory = !string.IsNullOrWhiteSpace(p?.Category) ? p.Category : "Produce",
                        Unit = !string.IsNullOrWhiteSpace(p?.Unit) ? p.Unit : "Kg",
                        ContractQuantity = qItem.Quantity,
                        SelectedVendorID = Quotation.VendorID,
                        SelectedVendorName = primaryVendorName
                    };

                    ProductLines.Add(line);
                    await LoadEligibleVendorsForProductLineAsync(line);

                    // Ensure quotation winner vendor is included in eligible vendors
                    if (!line.EligibleVendors.Any(v => v.VendorID == Quotation.VendorID) && primaryVendor != null)
                    {
                        line.EligibleVendors.Insert(0, new VendorRecommendationDto
                        {
                            VendorID = primaryVendor.VendorID,
                            VendorName = primaryVendor.VendorName,
                            ProductID = line.ProductID,
                            ProductName = line.ProductName,
                            UnitPrice = 0m
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CreateContract] LoadQuotationData error: {ex.Message}");
            ErrorMessage = "An error occurred while loading contract specifications from quotation.";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    // =========================================================================
    // SUBMISSION LOGIC (MULTI-PRODUCT ASSIGNMENTS)
    // =========================================================================
    private async Task SubmitContractCreation()
    {
        if (SelectedOutletId <= 0)
        {
            ValidationMessage = "Please select an outlet.";
            return;
        }

        if (ProductLines.Count == 0)
        {
            ValidationMessage = "Please add at least one product line to the contract.";
            return;
        }

        var unassigned = ProductLines.FirstOrDefault(p => p.SelectedVendorID <= 0);
        if (unassigned != null)
        {
            ValidationMessage = $"Please assign an eligible vendor for '{unassigned.ProductName}'.";
            return;
        }

        var zeroQty = ProductLines.FirstOrDefault(p => p.ContractQuantity <= 0);
        if (zeroQty != null)
        {
            ValidationMessage = $"Planning quantity for '{zeroQty.ProductName}' must be greater than zero.";
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

        IsProcessing = true;
        ValidationMessage = null;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            // Build multi-product assignments for Phase 2 backend
            var assignments = ProductLines.Select(p => new ContractProductAssignmentDto
            {
                ProductID = p.ProductID,
                VendorID = p.SelectedVendorID,
                ContractQuantity = p.ContractQuantity
            }).ToList();

            // Assemble CreateContractCommand with assignments and legacy backward-compatibility fields
            var command = new CreateContractCommand
            {
                QuotationID = Quotation?.QuotationID,
                OutletID = SelectedOutletId,
                StartDate = StartDate,
                EndDate = EndDate,
                PaymentMethod = PaymentMethod,
                Assignments = assignments,
                // Legacy fields preserved for backward compatibility
                ProductID = assignments.FirstOrDefault()?.ProductID ?? 0,
                TotalQuantity = assignments.Sum(a => a.ContractQuantity),
                Allocations = assignments.Select(a => new CreateContractVendorAllocationDto
                {
                    VendorID = a.VendorID,
                    AllocationPercentage = 100m
                }).ToList()
            };

            var response = await Api.CreateContractFullAsync(command);
            var createdContract = response?.Contract ?? response?.Contracts?.FirstOrDefault();

            if (createdContract != null && createdContract.ContractID > 0)
            {
                // Multi-contract vs single-contract navigation
                if (response?.Contracts != null && response.Contracts.Count > 1)
                {
                    Nav.NavigateTo("/organization/contracts");
                }
                else
                {
                    Nav.NavigateTo($"/contract/{createdContract.ContractID}");
                }
            }
            else
            {
                // Fallback attempt via standard Api.CreateContractAsync
                var fallbackContract = await Api.CreateContractAsync(command);
                if (fallbackContract != null && fallbackContract.ContractID > 0)
                {
                    Nav.NavigateTo($"/contract/{fallbackContract.ContractID}");
                }
                else
                {
                    ErrorMessage = "Failed to create contracts. Please ensure selected vendors actively supply the assigned products and try again.";
                }
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

    // =========================================================================
    // VENDOR EVALUATION & REVIEWS MODAL SUPPORT
    // =========================================================================
    private async Task OpenVendorReviewsModal(VendorRecommendationDto vendor, int productId, string productName)
    {
        SelectedVendorForReviews = vendor;
        SelectedProductForReviewsId = productId;
        SelectedProductForReviewsName = productName;
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
                var reviews = await Api.GetVendorReviewsAsync(vendor.VendorID, productId);
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