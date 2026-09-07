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
    // Mode & View States
    private bool IsCreateMode { get; set; } = false;
    private int CurrentStep { get; set; } = 1; // 1: Outlet, 2: Product, 3: Quantity & Recs, 4: Review

    // List View State
    private List<PurchaseRequestDto> AllPurchaseRequests { get; set; } = new();
    private List<PurchaseRequestDto> ScopedPurchaseRequests { get; set; } = new();
    private string SearchListQuery { get; set; } = string.Empty;
    private string StatusFilter { get; set; } = "All";
    private bool IsLoadingList { get; set; } = true;
    private bool HasListError { get; set; } = false;

    // Dictionaries for lookups
    private Dictionary<int, string> ProductNamesDict { get; set; } = new();
    private Dictionary<int, string> VendorNamesDict { get; set; } = new();
    private Dictionary<int, string> OutletNamesDict { get; set; } = new();
    private Dictionary<int, QuotationDto> QuotationsByPrDict { get; set; } = new();

    // Data for Creation Wizard
    private List<OutletDto> Outlets { get; set; } = new();
    private List<ProductDto> AllProducts { get; set; } = new();

    private int SelectedOutletId { get; set; } = 0;
    private string ConfirmedOutletName { get; set; } = string.Empty;
    private string OrganizationName { get; set; } = string.Empty;
    private string OutletAddress { get; set; } = string.Empty;
    private bool IsOutletConfirmed { get; set; } = false;

    private ProductDto? SelectedProduct { get; set; }
    private string SearchProductQuery { get; set; } = string.Empty;

    private decimal? QuantityInput { get; set; } = 10;
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

    // Navigation & Shell State
    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;
    private bool ShowNotificationDropdown { get; set; } = false;
    private bool ShowProfileModal { get; set; } = false;

    private PurchaseRequestDto? CreatedPurchaseRequest { get; set; }
    private List<VendorRecommendationDto>? VendorRecommendations { get; set; }
    private List<VendorRecommendationDto> SelectedVendors { get; set; } = new();
    private Dictionary<int, VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto> LatestReviewsByVendorDict { get; set; } = new();
    private string VendorSortBy { get; set; } = "Reviews";

    private List<VendorRecommendationDto> SortedVendorRecommendations
    {
        get
        {
            if (VendorRecommendations == null) return new();
            return VendorSortBy switch
            {
                "Price" => VendorRecommendations
                    .OrderBy(r => r.UnitPrice)
                    .ThenByDescending(r => r.AverageRating)
                    .ToList(),
                "Reviews" => VendorRecommendations
                    .OrderByDescending(r => r.AverageRating)
                    .ThenByDescending(r => r.TotalFeedbackCount)
                    .ThenBy(r => r.UnitPrice)
                    .ToList(),
                _ => VendorRecommendations
            };
        }
    }

    // Notifications
    private List<NotificationDto> Notifications { get; set; } = new();
    private int UnreadNotificationCount => Notifications.Count(n => !n.IsRead);

    private string UserInitial => !string.IsNullOrWhiteSpace(Auth.UserName)
        ? Auth.UserName.Substring(0, 1).ToUpperInvariant()
        : "P";

    private List<ProductDto> FilteredProducts
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchProductQuery))
            {
                return AllProducts;
            }
            return AllProducts.Where(p => p.ProductName.Contains(SearchProductQuery, StringComparison.OrdinalIgnoreCase) ||
                                          (p.Category != null && p.Category.Contains(SearchProductQuery, StringComparison.OrdinalIgnoreCase))).ToList();
        }
    }

    private List<PurchaseRequestDto> FilteredPurchaseRequests
    {
        get
        {
            var list = ScopedPurchaseRequests.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                list = list.Where(r => string.Equals(r.Status, StatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SearchListQuery))
            {
                list = list.Where(r =>
                    $"PR-{r.RequestID}".Contains(SearchListQuery, StringComparison.OrdinalIgnoreCase) ||
                    (r.Items != null && r.Items.Any(i => i.ProductName != null && i.ProductName.Contains(SearchListQuery, StringComparison.OrdinalIgnoreCase))) ||
                    (r.OutletName != null && r.OutletName.Contains(SearchListQuery, StringComparison.OrdinalIgnoreCase)));
            }

            return list.OrderByDescending(r => r.RequestDate).ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (Auth.IsAuthenticated && (Auth.IsPurchaseManager || Auth.IsAdmin))
        {
            await LoadInitialDataAsync();
        }
        else
        {
            IsLoadingList = false;
            IsLoadingOutlets = false;
        }
    }

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ToggleProfileDropdown()
    {
        IsProfileDropdownOpen = !IsProfileDropdownOpen;
        if (IsProfileDropdownOpen) ShowNotificationDropdown = false;
    }

    private void ToggleNotifications()
    {
        ShowNotificationDropdown = !ShowNotificationDropdown;
        if (ShowNotificationDropdown) IsProfileDropdownOpen = false;
    }

    private void OpenProfileModal()
    {
        ShowProfileModal = true;
        IsProfileDropdownOpen = false;
    }

    private void CloseProfileModal()
    {
        ShowProfileModal = false;
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/purchase-manager/login", true);
    }

    private async Task MarkNotificationRead(int notificationId)
    {
        await Api.MarkNotificationReadAsync(notificationId);
        var notif = Notifications.FirstOrDefault(n => n.NotificationID == notificationId);
        if (notif != null) notif.IsRead = true;
        StateHasChanged();
    }

    private async Task LoadInitialDataAsync()
    {
        IsLoadingList = true;
        HasListError = false;
        StateHasChanged();

        try
        {
            var requestsTask = Api.GetPurchaseRequestsAsync();
            var productsTask = Api.GetProductsAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var outletsTask = Api.GetOutletsAsync();
            var orgsTask = Api.GetOrganizationsAsync();
            var quotationsTask = Api.GetQuotationsAsync();
            var notifsTask = Api.GetMyNotificationsAsync();

            await Task.WhenAll(requestsTask, productsTask, vendorsTask, outletsTask, orgsTask, quotationsTask, notifsTask);

            AllPurchaseRequests = await requestsTask ?? new List<PurchaseRequestDto>();
            AllProducts = await productsTask ?? new List<ProductDto>();
            var allVendors = await vendorsTask ?? new List<VendorDto>();
            Outlets = await outletsTask ?? new List<OutletDto>();
            var allOrgs = await orgsTask ?? new List<OrganizationDto>();
            var allQuotations = await quotationsTask ?? new List<QuotationDto>();
            Notifications = await notifsTask ?? new List<NotificationDto>();

            // Build Dictionaries
            ProductNamesDict = AllProducts.ToDictionary(p => p.ProductID, p => p.ProductName);
            VendorNamesDict = allVendors.ToDictionary(v => v.VendorID, v => v.VendorName);
            OutletNamesDict = Outlets.ToDictionary(o => o.OutletID, o => GetDisplayOutletName(o));
            QuotationsByPrDict = allQuotations.GroupBy(q => q.RequestID).ToDictionary(g => g.Key, g => g.First());

            // Scope Requests to PM's outlet
            if (Auth.OutletID.HasValue && Auth.OutletID.Value > 0)
            {
                ScopedPurchaseRequests = AllPurchaseRequests.Where(r => r.OutletID == Auth.OutletID.Value).ToList();
                var matched = Outlets.FirstOrDefault(o => o.OutletID == Auth.OutletID.Value);
                if (matched != null)
                {
                    SelectedOutletId = matched.OutletID;
                    ConfirmedOutletName = GetDisplayOutletName(matched);
                    OutletAddress = matched.Address ?? string.Empty;
                }
                else
                {
                    SelectedOutletId = Auth.OutletID.Value;
                    ConfirmedOutletName = $"Outlet #{Auth.OutletID.Value}";
                }
            }
            else
            {
                ScopedPurchaseRequests = AllPurchaseRequests;
                if (Outlets.Count > 0)
                {
                    SelectedOutletId = Outlets[0].OutletID;
                    ConfirmedOutletName = GetDisplayOutletName(Outlets[0]);
                    OutletAddress = Outlets[0].Address ?? string.Empty;
                }
            }

            // Resolve Org Name
            if (Auth.OrganizationID.HasValue && Auth.OrganizationID.Value > 0)
            {
                var matchedOrg = allOrgs.FirstOrDefault(o => o.OrganizationID == Auth.OrganizationID.Value);
                OrganizationName = matchedOrg?.OrganizationName ?? $"Organization #{Auth.OrganizationID.Value}";
            }
            else if (allOrgs.Count > 0)
            {
                OrganizationName = allOrgs[0].OrganizationName;
            }
            else
            {
                OrganizationName = "Smart Vendor Enterprise";
            }

            IsOutletConfirmed = SelectedOutletId > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Procurement] Error loading initial data: {ex.Message}");
            HasListError = true;
        }
        finally
        {
            IsLoadingList = false;
            IsLoadingOutlets = false;
            StateHasChanged();
        }
    }

    // --- MODE SWITCHING ---
    private void StartCreateRequest()
    {
        IsCreateMode = true;
        CurrentStep = 1;
        SelectedProduct = null;
        QuantityInput = 10;
        CreatedPurchaseRequest = null;
        VendorRecommendations = null;
        SelectedVendors.Clear();
        LatestReviewsByVendorDict.Clear();
        PurchaseRequestError = null;
        RecommendationsError = null;
        DispatchSuccessMessage = null;
        SearchProductQuery = string.Empty;
    }

    private void CancelCreateRequest()
    {
        IsCreateMode = false;
        CurrentStep = 1;
        SelectedProduct = null;
        CreatedPurchaseRequest = null;
        VendorRecommendations = null;
        SelectedVendors.Clear();
        LatestReviewsByVendorDict.Clear();
        PurchaseRequestError = null;
        RecommendationsError = null;
        DispatchSuccessMessage = null;
    }

    private async Task GoToStep2Product()
    {
        ValidationMessage = string.Empty;
        if (SelectedOutletId <= 0)
        {
            ValidationMessage = "Assigned outlet not detected. Please contact your administrator.";
            return;
        }
        CurrentStep = 2;
        if (AllProducts.Count == 0)
        {
            await LoadProductsAsync();
        }
    }

    private async Task LoadProductsAsync()
    {
        IsLoadingProducts = true;
        HasErrorProducts = false;
        StateHasChanged();
        try
        {
            AllProducts = await Api.GetProductsAsync() ?? new List<ProductDto>();
            ProductNamesDict = AllProducts.ToDictionary(p => p.ProductID, p => p.ProductName);
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
        CurrentStep = 3;
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

    private void GoToStep4Review()
    {
        QuantityValidationMessage = string.Empty;
        if (!QuantityInput.HasValue || QuantityInput.Value <= 0)
        {
            QuantityValidationMessage = "Please enter a valid quantity greater than 0.";
            return;
        }
        if (SelectedVendors.Count == 0)
        {
            QuantityValidationMessage = "Please select at least one vendor to receive this purchase request.";
            return;
        }
        CurrentStep = 4;
    }

    private void GoBackToStep(int step)
    {
        CurrentStep = step;
        PurchaseRequestError = null;
        QuantityValidationMessage = string.Empty;
    }

    private void OnQuantityChanged()
    {
        QuantityValidationMessage = string.Empty;
    }

    private async Task FetchVendorRecommendations()
    {
        if (SelectedProduct == null || SelectedOutletId <= 0) return;

        IsFindingVendors = true;
        RecommendationsError = null;
        StateHasChanged();

        try
        {
            var vendorProducts = await Api.GetVendorProductsAsync();
            var vendors = await Api.GetVendorsAsync();
            var contracts = await Api.GetContractsAsync();
            var performances = await Api.GetVendorPerformanceSummariesAsync();

            var matchingVP = vendorProducts?.Where(vp =>
                vp.ProductID == SelectedProduct.ProductID &&
                string.Equals(vp.Status, "Active", StringComparison.OrdinalIgnoreCase)).ToList() ?? new();

            var vendorDict = vendors?.ToDictionary(v => v.VendorID, v => v.VendorName) ?? new();
            var perfDict = performances?.ToDictionary(p => p.VendorID) ?? new();

            // Concurrently fetch product-specific reviews for candidate vendors
            var reviewTasks = matchingVP
                .Select(vp => vp.VendorID)
                .Distinct()
                .ToDictionary(
                    vId => vId,
                    vId => Api.GetVendorReviewsAsync(vId, SelectedProduct.ProductID)
                );

            await Task.WhenAll(reviewTasks.Values);

            var productReviewsByVendor = new Dictionary<int, List<VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto>>();
            foreach (var kvp in reviewTasks)
            {
                try
                {
                    productReviewsByVendor[kvp.Key] = (await kvp.Value) ?? new();
                }
                catch
                {
                    productReviewsByVendor[kvp.Key] = new();
                }
            }

            var recs = new List<VendorRecommendationDto>();
            decimal minPrice = matchingVP.Count > 0 ? matchingVP.Min(vp => vp.UnitPrice) : 0m;

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

                perfDict.TryGetValue(vp.VendorID, out var perf);
                int perfFeedbackCount = perf?.TotalReviews ?? 0;
                int completedDeliveries = perf?.CompletedDeliveries ?? 0;
                decimal? spoilageRate = perf?.SpoilageRate;
                decimal perfAvgQuality = (perf != null && perf.AverageQualityRating.HasValue) ? perf.AverageQualityRating.Value : 0m;
                decimal perfAvgDelivery = (perf != null && perf.AverageDeliveryRating.HasValue) ? perf.AverageDeliveryRating.Value : 0m;

                decimal qualityScore = perfFeedbackCount > 0 ? (perfAvgQuality / 5.0m) * 100m : 70m;
                decimal deliveryScore = perfFeedbackCount > 0 ? (perfAvgDelivery / 5.0m) * 100m : 70m;
                decimal priceScore = (vp.UnitPrice > 0 && minPrice > 0)
                    ? Math.Round((minPrice / vp.UnitPrice) * 100m, 1)
                    : 70m;
                decimal reliabilityBonus = Math.Min(15m, perfFeedbackCount * 3m);

                decimal overallCompositeScore = Math.Round(
                    (qualityScore * 0.35m) +
                    (deliveryScore * 0.25m) +
                    (priceScore * 0.25m) +
                    reliabilityBonus,
                    1
                );

                // Product-specific review calculations
                productReviewsByVendor.TryGetValue(vp.VendorID, out var pReviews);
                pReviews ??= new List<VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto>();

                int productFeedbackCount = pReviews.Count;
                decimal productAvgRating = productFeedbackCount > 0 ? Math.Round(pReviews.Average(r => r.Rating), 1) : 0m;
                decimal productAvgQuality = productFeedbackCount > 0 ? Math.Round(pReviews.Average(r => r.ProductQualityRating), 1) : perfAvgQuality;
                decimal productAvgDelivery = productFeedbackCount > 0 ? Math.Round(pReviews.Average(r => r.DeliveryRating), 1) : perfAvgDelivery;

                var latestReview = pReviews.FirstOrDefault();
                if (latestReview != null)
                {
                    LatestReviewsByVendorDict[vp.VendorID] = latestReview;
                }

                recs.Add(new VendorRecommendationDto
                {
                    VendorID = vp.VendorID,
                    VendorName = vName,
                    ProductID = SelectedProduct.ProductID,
                    ProductName = SelectedProduct.ProductName,
                    UnitPrice = vp.UnitPrice,
                    EstimatedDeliveryDays = vp.EstimatedDeliveryDays,
                    OverallScore = overallCompositeScore,
                    AverageRating = productAvgRating,
                    AverageQualityRating = productAvgQuality,
                    AverageDeliveryRating = productAvgDelivery,
                    TotalFeedbackCount = productFeedbackCount,
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
                        rec.Recommendation = $"{rec.VendorName} is recommended over other suppliers ({compNames}) with a verified {rec.AverageQualityRating:0.0}/5 Quality rating across {deliveredCount} delivered orders with {spoilageText}.";
                    }
                    else if (rec.HasActiveContract && rec.RemainingQuantity > 0)
                    {
                        rec.Recommendation = $"{rec.VendorName} is recommended with priority active contract allocation ({rec.RemainingQuantity:N0} {SelectedProduct.Unit} remaining) and a verified {rec.AverageQualityRating:0.0}/5 Quality rating.";
                    }
                    else
                    {
                        rec.Recommendation = $"{rec.VendorName} is recommended with the highest overall performance score and verified {rec.AverageQualityRating:0.0}/5 Quality rating.";
                    }
                }
                else if (rec.HasActiveContract && rec.RemainingQuantity > 0)
                {
                    rec.Recommendation = "Active Contract Allocation";
                }
                else if (rec.UnitPrice == minPrice)
                {
                    rec.Recommendation = $"Lowest unit price (Rs. {rec.UnitPrice:N2}) with {rec.AverageRating:0.0}★ customer rating.";
                }
                else
                {
                    rec.Recommendation = "Alternative supplier candidate.";
                }
            }

            if (VendorRecommendations.Count > 0 && SelectedVendors.Count == 0)
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

    private async Task DispatchToSelectedVendors()
    {
        if (!Auth.IsPurchaseManager && !Auth.IsAdmin) return;
        if (SelectedProduct == null || SelectedVendors.Count == 0 || !QuantityInput.HasValue || QuantityInput.Value <= 0) return;

        IsDispatching = true;
        PurchaseRequestError = null;
        DispatchSuccessMessage = null;
        StateHasChanged();

        try
        {
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
                PurchaseRequestError = "Unable to create Purchase Request.";
                IsDispatching = false;
                StateHasChanged();
                return;
            }

            var selectedIds = SelectedVendors.Select(v => v.VendorID).ToList();
            var result = await Api.DispatchPurchaseRequestAsync(CreatedPurchaseRequest.RequestID, selectedIds);

            if (result != null && result.Success)
            {
                string vendorNames = string.Join(", ", SelectedVendors.Select(v => $"'{v.VendorName}'"));
                DispatchSuccessMessage = $"Purchase Request PR-{CreatedPurchaseRequest.RequestID} has been successfully created and sent to {vendorNames}.";

                // Refresh the local purchase requests list in background
                _ = Task.Run(async () =>
                {
                    var updated = await Api.GetPurchaseRequestsAsync();
                    if (updated != null)
                    {
                        AllPurchaseRequests = updated;
                        if (Auth.OutletID.HasValue && Auth.OutletID.Value > 0)
                        {
                            ScopedPurchaseRequests = AllPurchaseRequests.Where(r => r.OutletID == Auth.OutletID.Value).ToList();
                        }
                        else
                        {
                            ScopedPurchaseRequests = AllPurchaseRequests;
                        }
                    }
                });
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
            return $"{outlet.Address.Split(',')[0].Trim()} Outlet";
        }
        return $"Outlet #{outlet.OutletID}";
    }

    private string GetPrProductDisplay(PurchaseRequestDto pr)
    {
        if (pr.Items != null && pr.Items.Count > 0)
        {
            var first = pr.Items[0];
            string pName = !string.IsNullOrWhiteSpace(first.ProductName)
                ? first.ProductName
                : (ProductNamesDict.TryGetValue(first.ProductID, out var n) ? n : "Item");

            if (pr.Items.Count > 1)
            {
                return $"{pName} (+{pr.Items.Count - 1} more)";
            }
            return pName;
        }
        return "Procurement Items";
    }

    private string GetPrQuantityDisplay(PurchaseRequestDto pr)
    {
        if (pr.Items != null && pr.Items.Count > 0)
        {
            var first = pr.Items[0];
            return $"{first.Quantity:G29} {first.Unit}".Trim();
        }
        return "—";
    }

    private string GetPrVendorDisplay(PurchaseRequestDto pr)
    {
        if (QuotationsByPrDict.TryGetValue(pr.RequestID, out var q))
        {
            string vName = VendorNamesDict.TryGetValue(q.VendorID, out var n) ? n : "Supplier";
            return $"{vName} ({q.Status})";
        }
        if (!string.IsNullOrWhiteSpace(pr.VendorName))
        {
            return pr.VendorName;
        }
        return "Awaiting Quotation";
    }

    private static string GetStatusBadgeClass(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "status-badge-neutral";
        return status.ToLowerInvariant() switch
        {
            "approved" or "accepted" or "completed" or "delivered" or "dispatched" => "status-badge-green",
            "pending" or "submitted" or "created" => "status-badge-green",
            "rejected" or "declined" or "cancelled" => "status-badge-neutral",
            _ => "status-badge-neutral"
        };
    }

    // --- VENDOR REVIEWS MODAL ---
    private bool IsReviewsModalOpen { get; set; } = false;
    private bool IsLoadingVendorReviews { get; set; } = false;
    private VendorRecommendationDto? SelectedVendorForReviews { get; set; }
    private List<VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto> CurrentVendorReviews { get; set; } = new();

    private async Task OpenVendorReviewsModal(VendorRecommendationDto vendor)
    {
        SelectedVendorForReviews = vendor;
        IsReviewsModalOpen = true;
        IsLoadingVendorReviews = true;
        CurrentVendorReviews.Clear();
        StateHasChanged();

        try
        {
            int? productId = vendor.ProductID > 0 ? vendor.ProductID : SelectedProduct?.ProductID;
            var reviews = await Api.GetVendorReviewsAsync(vendor.VendorID, productId);
            CurrentVendorReviews = reviews ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Procurement] Error loading reviews for vendor {vendor.VendorID}: {ex.Message}");
        }
        finally
        {
            IsLoadingVendorReviews = false;
            StateHasChanged();
        }
    }

    private void CloseVendorReviewsModal()
    {
        IsReviewsModalOpen = false;
        SelectedVendorForReviews = null;
        CurrentVendorReviews.Clear();
    }

    private string GetQualitativeRatingText(decimal rating)
    {
        if (rating >= 4.6m) return "Excellent";
        if (rating >= 4.0m) return "Very Good";
        if (rating >= 3.0m) return "Good";
        if (rating >= 2.0m) return "Average";
        return "Poor";
    }

    private decimal CurrentVendorAvgRating => CurrentVendorReviews.Count > 0 ? (decimal)Math.Round(CurrentVendorReviews.Average(r => r.Rating), 1) : 0m;
    private decimal CurrentVendorAvgQuality => CurrentVendorReviews.Count > 0 ? (decimal)Math.Round(CurrentVendorReviews.Average(r => r.ProductQualityRating), 1) : 0m;
    private decimal CurrentVendorAvgDelivery => CurrentVendorReviews.Count > 0 ? (decimal)Math.Round(CurrentVendorReviews.Average(r => r.DeliveryRating), 1) : 0m;
    private VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto? LatestReviewInModal => CurrentVendorReviews.FirstOrDefault();

    private static string GetStarString(decimal rating)
    {
        int rounded = (int)Math.Round(rating, MidpointRounding.AwayFromZero);
        rounded = Math.Clamp(rounded, 1, 5);
        return new string('★', rounded) + new string('☆', 5 - rounded);
    }
}