using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.Procurement;

public partial class Procurement : ComponentBase
{
    private List<OutletDto> Outlets { get; set; } = new();
    private List<ProductDto> AllProducts { get; set; } = new();

    private int SelectedOutletId { get; set; } = 0;
    private string? ConfirmedOutletName { get; set; }
    private string? OrganizationName { get; set; }
    private bool IsOutletConfirmed { get; set; } = false;

    private ProductDto? SelectedProduct { get; set; }
    private string SearchQuery { get; set; } = string.Empty;

    private decimal? QuantityInput { get; set; }
    private bool IsQuantityConfirmed { get; set; } = false;

    private string ValidationMessage { get; set; } = string.Empty;
    private string QuantityValidationMessage { get; set; } = string.Empty;

    private bool IsLoadingOutlets { get; set; } = true;
    private bool HasErrorOutlets { get; set; } = false;

    private bool IsLoadingProducts { get; set; } = false;
    private bool HasErrorProducts { get; set; } = false;

    private bool IsFindingVendors { get; set; } = false;
    private bool IsDispatching { get; set; } = false;
    private string? PurchaseRequestError { get; set; }
    private string? RecommendationsError { get; set; }
    private string? DispatchSuccessMessage { get; set; }

    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;

    private PurchaseRequestDto? CreatedPurchaseRequest { get; set; }
    private List<VendorRecommendationDto>? VendorRecommendations { get; set; }
    private List<VendorRecommendationDto> SelectedVendors { get; set; } = new();

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ToggleProfileDropdown()
    {
        IsProfileDropdownOpen = !IsProfileDropdownOpen;
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

    private List<ProductDto> FilteredProducts
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                return AllProducts;
            }
            return AllProducts.Where(p => p.ProductName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                                          (p.Category != null && p.Category.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))).ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (Auth.IsAuthenticated && (Auth.IsOrgManager || Auth.IsOutletManager || Auth.IsPurchaseManager || Auth.IsAdmin))
        {
            await LoadOutlets();
        }
        else
        {
            IsLoadingOutlets = false;
        }
    }

    private async Task LoadOutlets()
    {
        IsLoadingOutlets = true;
        HasErrorOutlets = false;
        StateHasChanged();

        try
        {
            if ((Auth.IsOutletManager || Auth.IsPurchaseManager) && Auth.OutletID.HasValue)
            {
                var rawOutlets = await Api.GetOutletsAsync() ?? new List<OutletDto>();
                var matched = rawOutlets.FirstOrDefault(o => o.OutletID == Auth.OutletID.Value);

                if (matched != null)
                {
                    Outlets = new List<OutletDto> { matched };
                    SelectedOutletId = matched.OutletID;
                    ConfirmedOutletName = GetDisplayOutletName(matched);
                    OrganizationName = Auth.OrganizationID.HasValue ? $"Organization #{Auth.OrganizationID.Value}" : "Assigned Organization";
                }
                else
                {
                    SelectedOutletId = Auth.OutletID.Value;
                    ConfirmedOutletName = $"Outlet #{Auth.OutletID.Value}";
                    OrganizationName = Auth.OrganizationID.HasValue ? $"Organization #{Auth.OrganizationID.Value}" : "Assigned Organization";
                }
            }
            else
            {
                var orgsTask = Api.GetOrganizationsAsync();
                var outletsTask = Auth.OrganizationID.HasValue
                    ? Api.GetOutletsByOrgIdAsync(Auth.OrganizationID.Value)
                    : Api.GetOutletsAsync();

                await Task.WhenAll(orgsTask, outletsTask);

                var orgs = await orgsTask;
                if (Auth.OrganizationID.HasValue && orgs != null)
                {
                    var matchingOrg = orgs.FirstOrDefault(o => o.OrganizationID == Auth.OrganizationID.Value);
                    OrganizationName = matchingOrg?.OrganizationName ?? $"Organization #{Auth.OrganizationID.Value}";
                }
                else if (orgs != null && orgs.Count > 0)
                {
                    OrganizationName = orgs[0].OrganizationName;
                }

                var rawOutlets = await outletsTask;
                if (Auth.OrganizationID.HasValue && rawOutlets != null)
                {
                    Outlets = rawOutlets.Where(o => o.OrganizationID == Auth.OrganizationID.Value).ToList();
                }
                else
                {
                    Outlets = rawOutlets ?? new List<OutletDto>();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Procurement] Error loading outlets: {ex.Message}");
            HasErrorOutlets = true;
        }
        finally
        {
            IsLoadingOutlets = false;
            StateHasChanged();
        }
    }

    private void OnOutletSelected()
    {
        ValidationMessage = string.Empty;
    }

    private async Task HandleContinue()
    {
        ValidationMessage = string.Empty;
        if (SelectedOutletId <= 0)
        {
            ValidationMessage = "Please select an outlet.";
            ConfirmedOutletName = null;
            IsOutletConfirmed = false;
            return;
        }

        var selected = Outlets.FirstOrDefault(o => o.OutletID == SelectedOutletId);
        if (selected != null)
        {
            ConfirmedOutletName = GetDisplayOutletName(selected);
        }
        else if ((Auth.IsOutletManager || Auth.IsPurchaseManager) && Auth.OutletID.HasValue)
        {
            ConfirmedOutletName = $"Outlet #{Auth.OutletID.Value}";
        }

        IsOutletConfirmed = true;
        await LoadProducts();
    }

    private void ChangeOutlet()
    {
        if (Auth.IsOutletManager || Auth.IsPurchaseManager)
        {
            return;
        }
        IsOutletConfirmed = false;
        SelectedProduct = null;
        QuantityInput = null;
        IsQuantityConfirmed = false;
        CreatedPurchaseRequest = null;
        VendorRecommendations = null;
        SelectedVendors.Clear();
        ValidationMessage = string.Empty;
        QuantityValidationMessage = string.Empty;
        PurchaseRequestError = null;
        RecommendationsError = null;
        DispatchSuccessMessage = null;
    }

    private async Task LoadProducts()
    {
        IsLoadingProducts = true;
        HasErrorProducts = false;
        StateHasChanged();

        try
        {
            AllProducts = await Api.GetProductsAsync();
        }
        catch (Exception)
        {
            HasErrorProducts = true;
        }
        finally
        {
            IsLoadingProducts = false;
            StateHasChanged();
        }
    }

    private async Task SelectProduct(ProductDto product)
    {
        SelectedProduct = product;
        if (!QuantityInput.HasValue || QuantityInput.Value <= 0)
        {
            QuantityInput = 10;
        }
        IsQuantityConfirmed = false;
        CreatedPurchaseRequest = null;
        VendorRecommendations = null;
        SelectedVendors.Clear();
        QuantityValidationMessage = string.Empty;
        PurchaseRequestError = null;
        RecommendationsError = null;
        DispatchSuccessMessage = null;

        await FetchVendorRecommendations();

        if (VendorRecommendations != null && VendorRecommendations.Count > 0 && SelectedVendors.Count == 0)
        {
            SelectedVendors.Add(VendorRecommendations[0]);
        }
    }

    private void ChangeProduct()
    {
        SelectedProduct = null;
        QuantityInput = null;
        IsQuantityConfirmed = false;
        CreatedPurchaseRequest = null;
        VendorRecommendations = null;
        SelectedVendors.Clear();
        QuantityValidationMessage = string.Empty;
        PurchaseRequestError = null;
        RecommendationsError = null;
        DispatchSuccessMessage = null;
    }

    private void OnQuantityChanged()
    {
        QuantityValidationMessage = string.Empty;
        IsQuantityConfirmed = false;
    }

    private async Task HandleFindVendors()
    {
        QuantityValidationMessage = string.Empty;
        PurchaseRequestError = null;
        RecommendationsError = null;
        DispatchSuccessMessage = null;

        if (!QuantityInput.HasValue || QuantityInput.Value <= 0)
        {
            QuantityValidationMessage = "Quantity must be greater than 0.";
            return;
        }

        if (SelectedProduct == null || SelectedOutletId <= 0)
        {
            return;
        }

        await FetchVendorRecommendations();
    }

    private async Task FetchVendorRecommendations()
    {
        if (SelectedProduct == null || SelectedOutletId <= 0) return;

        IsFindingVendors = true;
        RecommendationsError = null;
        StateHasChanged();

        try
        {
            if (CreatedPurchaseRequest != null)
            {
                var response = await Api.GetVendorRecommendationsAsync(CreatedPurchaseRequest.RequestID);
                if (response != null && response.Recommendations != null)
                {
                    VendorRecommendations = response.Recommendations.OrderBy(r => r.Rank).ToList();
                }
            }
            else
            {
                // Query eligible vendors for this product and outlet directly without creating a premature 10kg draft
                var vendorProducts = await Api.GetVendorProductsAsync();
                var vendors = await Api.GetVendorsAsync();
                var contracts = await Api.GetContractsAsync();

                var matchingVP = vendorProducts?.Where(vp => 
                    vp.ProductID == SelectedProduct.ProductID && 
                    string.Equals(vp.Status, "Active", StringComparison.OrdinalIgnoreCase)).ToList() ?? new();

                var vendorDict = vendors?.ToDictionary(v => v.VendorID, v => v.VendorName) ?? new();

                var recs = new List<VendorRecommendationDto>();
                decimal minPrice = matchingVP.Count > 0 ? matchingVP.Min(vp => vp.UnitPrice) : 0m;

                // Fetch performance profiles for all active vendors to display verified ratings
                var performances = await Api.GetVendorPerformanceSummariesAsync();
                var perfDict = performances?.ToDictionary(p => p.VendorID) ?? new();

                foreach (var vp in matchingVP)
                {
                    string vName = vendorDict.TryGetValue(vp.VendorID, out var name) ? name : $"Vendor #{vp.VendorID}";
                    var matchingContract = contracts?.FirstOrDefault(c => 
                        (c.VendorID == vp.VendorID || (c.Allocations != null && c.Allocations.Any(a => a.VendorID == vp.VendorID))) &&
                        c.ProductID == SelectedProduct.ProductID &&
                        c.OutletID == SelectedOutletId &&
                        string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase));

                    bool hasContract = matchingContract != null;
                    decimal remainingQty = 0m;
                    if (matchingContract != null)
                    {
                        if (matchingContract.Allocations != null && matchingContract.Allocations.Count > 0)
                        {
                            var alloc = matchingContract.Allocations.FirstOrDefault(a => a.VendorID == vp.VendorID);
                            if (alloc != null)
                            {
                                remainingQty = Math.Max(0m, alloc.AllocatedQuantity - alloc.UsedQuantity);
                            }
                        }
                        else
                        {
                            remainingQty = Math.Max(0m, matchingContract.TotalQuantity - matchingContract.UsedQuantity);
                        }
                    }

                    bool hasUsableContract = hasContract && remainingQty > 0;

                    // Pull real rating metrics & delivery history
                    perfDict.TryGetValue(vp.VendorID, out var perf);
                    int feedbackCount = perf?.TotalReviews ?? 0;
                    int completedDeliveries = perf?.CompletedDeliveries ?? 0;
                    decimal? spoilageRate = perf?.SpoilageRate;
                    decimal avgRating = (perf != null && perf.AverageRating.HasValue) ? perf.AverageRating.Value : 0m;
                    decimal avgQuality = (perf != null && perf.AverageQualityRating.HasValue) ? perf.AverageQualityRating.Value : 0m;
                    decimal avgDelivery = (perf != null && perf.AverageDeliveryRating.HasValue) ? perf.AverageDeliveryRating.Value : 0m;

                    // Compute 100-point multi-factor score
                    decimal qualityScore = feedbackCount > 0 ? (avgQuality / 5.0m) * 100m : 70m;
                    decimal deliveryScore = feedbackCount > 0 ? (avgDelivery / 5.0m) * 100m : 70m;
                    decimal priceScore = (vp.UnitPrice > 0 && minPrice > 0)
                        ? Math.Round((minPrice / vp.UnitPrice) * 100m, 1)
                        : 70m;
                    decimal reliabilityBonus = Math.Min(15m, feedbackCount * 3m);

                    decimal overallCompositeScore = Math.Round(
                        (qualityScore * 0.35m) +
                        (deliveryScore * 0.25m) +
                        (priceScore * 0.25m) +
                        reliabilityBonus,
                        1
                    );

                    recs.Add(new VendorRecommendationDto
                    {
                        VendorID = vp.VendorID,
                        VendorName = vName,
                        ProductID = SelectedProduct.ProductID,
                        ProductName = SelectedProduct.ProductName,
                        UnitPrice = vp.UnitPrice,
                        EstimatedDeliveryDays = vp.EstimatedDeliveryDays,
                        OverallScore = overallCompositeScore,
                        AverageRating = avgRating,
                        AverageQualityRating = avgQuality,
                        AverageDeliveryRating = avgDelivery,
                        TotalFeedbackCount = feedbackCount,
                        CompletedDeliveries = completedDeliveries,
                        SpoilageRate = spoilageRate,
                        HasActiveContract = hasUsableContract,
                        RemainingQuantity = remainingQty
                    });
                }

                VendorRecommendations = recs
                    .OrderByDescending(r => r.HasActiveContract && r.RemainingQuantity > 0)
                    .ThenByDescending(r => r.OverallScore)
                    .ThenBy(r => r.UnitPrice)
                    .ThenBy(r => r.EstimatedDeliveryDays)
                    .ThenBy(r => r.VendorID)
                    .ToList();

                for (int i = 0; i < VendorRecommendations.Count; i++)
                {
                    var rec = VendorRecommendations[i];
                    rec.Rank = i + 1;
                    if (i == 0)
                    {
                        rec.SmartBadge = "Top Recommended";
                    }
                    else if (rec.AverageQualityRating >= 4.5m)
                    {
                        rec.SmartBadge = "Best Quality";
                    }
                    else if (rec.UnitPrice == minPrice)
                    {
                        rec.SmartBadge = "Best Price";
                    }
                    else if (rec.AverageDeliveryRating >= 4.5m)
                    {
                        rec.SmartBadge = "Fastest Delivery";
                    }

                    if (i == 0)
                    {
                        // Generate rich comparative AI recommendation from database metrics
                        var competitors = VendorRecommendations.Where(r => r.VendorID != rec.VendorID).ToList();
                        string compNames = competitors.Count > 0
                            ? string.Join(", ", competitors.Select(c => c.VendorName))
                            : "other suppliers";

                        decimal minOtherPrice = competitors.Count > 0 ? competitors.Min(c => c.UnitPrice) : rec.UnitPrice;
                        decimal priceDiff = rec.UnitPrice - minOtherPrice;

                        string spoilageText = rec.SpoilageRate.HasValue
                            ? $"{rec.SpoilageRate.Value:0.0}% spoilage"
                            : "0% reported spoilage";

                        int deliveredCount = rec.CompletedDeliveries > 0 ? rec.CompletedDeliveries : Math.Max(1, rec.TotalFeedbackCount);

                        if (priceDiff > 0 && competitors.Count > 0)
                        {
                            rec.Recommendation = $"{rec.VendorName} is recommended over other suppliers ({compNames}) because, despite a Rs. {priceDiff:N2} / {SelectedProduct.Unit} price difference, {rec.VendorName} has a proven {rec.AverageQualityRating:0.0}/5 Quality rating across {deliveredCount} delivered orders with {spoilageText}, ensuring maximum product freshness and dependable kitchen operations.";
                        }
                        else if (rec.HasActiveContract && rec.RemainingQuantity > 0)
                        {
                            rec.Recommendation = $"{rec.VendorName} is recommended with priority active contract allocation ({rec.RemainingQuantity:N0} {SelectedProduct.Unit} remaining) and a verified {rec.AverageQualityRating:0.0}/5 Quality rating across {deliveredCount} delivered orders with {spoilageText}.";
                        }
                        else
                        {
                            rec.Recommendation = $"{rec.VendorName} is recommended over competitors ({compNames}) with the highest overall performance score, verified {rec.AverageQualityRating:0.0}/5 Quality rating across {deliveredCount} delivered orders with {spoilageText}.";
                        }
                    }
                    else if (rec.HasActiveContract && rec.RemainingQuantity > 0)
                    {
                        rec.Recommendation = "Active Contract";
                    }
                    else if (rec.UnitPrice == minPrice)
                    {
                        rec.Recommendation = rec.TotalFeedbackCount > 0
                            ? $"Alternative — Best Price: Lowest unit price (Rs. {rec.UnitPrice:N2}) with {rec.AverageRating:0.0}★ customer rating."
                            : $"Alternative — Best Price: Lowest unit price (Rs. {rec.UnitPrice:N2}).";
                    }
                    else
                    {
                        rec.Recommendation = rec.TotalFeedbackCount > 0 && rec.AverageQualityRating >= 4.0m
                            ? $"Alternative: High quality rating ({rec.AverageQualityRating:0.0}/5★) across {rec.TotalFeedbackCount} past deliveries."
                            : "Alternative";
                    }
                }
            }

            if (VendorRecommendations != null && SelectedVendors.Count == 0 && VendorRecommendations.Count > 0)
            {
                SelectedVendors.Add(VendorRecommendations[0]);
            }
            RecommendationsError = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Procurement] Error loading recommendations: {ex.Message}");
            VendorRecommendations = null;
            RecommendationsError = "Unable to load vendors due to a server connection issue.";
        }
        finally
        {
            IsFindingVendors = false;
            StateHasChanged();
        }
    }

    private void ToggleVendorSelection(VendorRecommendationDto vendor)
    {
        if (SelectedVendors.Any(v => v.VendorID == vendor.VendorID))
        {
            SelectedVendors.RemoveAll(v => v.VendorID == vendor.VendorID);
        }
        else
        {
            SelectedVendors.Clear();
            SelectedVendors.Add(vendor);
        }
    }

    private void ClearSelectedVendors()
    {
        SelectedVendors.Clear();
    }

    private async Task DispatchToSelectedVendors()
    {
        if (SelectedProduct == null || SelectedVendors.Count == 0 || !QuantityInput.HasValue || QuantityInput.Value <= 0) return;

        IsDispatching = true;
        PurchaseRequestError = null;
        DispatchSuccessMessage = null;
        StateHasChanged();

        try
        {
            // Create the single official purchase request with the exact typed quantity
            var command = new CreatePurchaseRequestCommand
            {
                OutletID = SelectedOutletId,
                CreatedByUserID = Auth.UserID,
                RequestDate = DateTime.Now,
                Items = new List<CreatePurchaseRequestItemDto>
                {
                    new CreatePurchaseRequestItemDto
                    {
                        ProductID = SelectedProduct.ProductID,
                        Quantity = QuantityInput.Value,
                        Unit = SelectedProduct.Unit
                    }
                }
            };

            CreatedPurchaseRequest = await Api.CreatePurchaseRequestAsync(command);
            if (CreatedPurchaseRequest == null)
            {
                PurchaseRequestError = "Unable to create Purchase Request with the specified quantity.";
                IsDispatching = false;
                StateHasChanged();
                return;
            }

            var selectedIds = SelectedVendors.Select(v => v.VendorID).ToList();
            var result = await Api.DispatchPurchaseRequestAsync(CreatedPurchaseRequest.RequestID, selectedIds);

            if (result != null && result.Success)
            {
                string vendorNames = string.Join(", ", SelectedVendors.Select(v => $"'{v.VendorName}'"));
                DispatchSuccessMessage = $"Purchase Request PR-{CreatedPurchaseRequest.RequestID} has been successfully sent to {vendorNames}. The respective Vendor Manager(s) have been notified and can now accept or reject the opportunity.";
            }
            else
            {
                PurchaseRequestError = result?.Message ?? "Unable to dispatch Purchase Request to selected vendor(s).";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Procurement] Dispatch error: {ex.Message}");
            PurchaseRequestError = "An error occurred while dispatching the Purchase Request.";
        }
        finally
        {
            IsDispatching = false;
            StateHasChanged();
        }
    }

    private string GetDisplayOutletName(OutletDto outlet)
    {
        if (!string.IsNullOrWhiteSpace(outlet.OutletName))
        {
            return outlet.OutletName;
        }
        if (!string.IsNullOrWhiteSpace(outlet.Address))
        {
            return $"{outlet.Address} Outlet";
        }
        return $"Outlet #{outlet.OutletID}";
    }

    // --- VENDOR REVIEWS MODAL ---
    private bool IsReviewsModalOpen { get; set; } = false;
    private bool IsLoadingVendorReviews { get; set; } = false;
    private bool IsLoadingAiInsights { get; set; } = false;
    private VendorRecommendationDto? SelectedVendorForReviews { get; set; }
    private List<VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto> CurrentVendorReviews { get; set; } = new();
    private VenodorManagementFrontend.Models.VendorPerformance.VendorAiInsightsDto? CurrentAiInsights { get; set; }

    private async Task OpenVendorReviewsModal(VendorRecommendationDto vendor)
    {
        SelectedVendorForReviews = vendor;
        IsReviewsModalOpen = true;
        IsLoadingVendorReviews = true;
        IsLoadingAiInsights = true;
        CurrentVendorReviews.Clear();
        CurrentAiInsights = null;
        StateHasChanged();

        try
        {
            var reviews = await Api.GetVendorReviewsAsync(vendor.VendorID);
            CurrentVendorReviews = reviews ?? new();
            IsLoadingVendorReviews = false;
            StateHasChanged();

            // Fetch Gemini AI analysis securely via Backend API
            try
            {
                CurrentAiInsights = await Api.GetVendorAiInsightsAsync(vendor.VendorID);
            }
            catch (Exception aiEx)
            {
                Console.WriteLine($"[Procurement] AI Insights load error: {aiEx.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Procurement] Error loading reviews for vendor {vendor.VendorID}: {ex.Message}");
        }
        finally
        {
            IsLoadingVendorReviews = false;
            IsLoadingAiInsights = false;
            StateHasChanged();
        }
    }

    private void CloseVendorReviewsModal()
    {
        IsReviewsModalOpen = false;
        SelectedVendorForReviews = null;
        CurrentVendorReviews.Clear();
        CurrentAiInsights = null;
    }

    private string GetQualitativeRatingText(decimal rating)
    {
        if (rating >= 4.6m) return "Excellent";
        if (rating >= 4.0m) return "Very Good";
        if (rating >= 3.0m) return "Good";
        if (rating >= 2.0m) return "Average";
        return "Poor";
    }

    private string SidebarLayoutClass =>
        IsSidebarCollapsed ? "org-app-layout sidebar-collapsed" : "org-app-layout";
}