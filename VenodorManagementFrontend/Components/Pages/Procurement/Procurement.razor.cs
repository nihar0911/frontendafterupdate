using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Models.VendorPerformance;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.Procurement;

public partial class Procurement : ComponentBase, IDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    // Mode & View States
    private bool IsCreateMode { get; set; } = false;
    private int CurrentStep { get; set; } = 1; // 1: Outlet, 2: Add Products & Cart, 3: Review & Dispatch

    // AI Procurement Assistant Isolated State
    private string ProcurementInputMode { get; set; } = "Manual"; // "Manual" or "AI"
    private string AiPromptInput { get; set; } = string.Empty;
    private bool IsAiParsing { get; set; } = false;
    private string? AiErrorMessage { get; set; }
    private ParseVoiceProcurementOrderResponse? AiResponse { get; set; }
    private Dictionary<int, List<VendorRecommendationDto>> AiItemRecommendations { get; set; } = new();
    private Dictionary<int, bool> AiItemHasActiveContracts { get; set; } = new();
    private Dictionary<int, bool> AiItemLoadingStates { get; set; } = new();
    private Dictionary<int, string?> AiItemErrorStates { get; set; } = new();
    private Dictionary<int, VendorRecommendationDto?> AiSelectedVendors { get; set; } = new();
    private HashSet<int> AiItemAddedToCartKeys { get; set; } = new();
    private string SelectedVoiceLanguage { get; set; } = "hi-IN"; // "hi-IN" or "en-US"
    private bool IsTtsEnabled { get; set; } = true;
    private string? AssistantSpeechMessage { get; set; }
    private bool IsVoiceListening { get; set; } = false;
    private string? VoiceMessage { get; set; }
    private DotNetObjectReference<Procurement>? _dotNetRef;

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

    // Multi-Product Procurement Cart State
    private List<ProcurementItem> CartItems { get; set; } = new();
    private string? EditingCartItemKey { get; set; }

    // Current Product Selection & Recommendation State
    private ProductDto? SelectedProduct { get; set; }
    private string SearchProductQuery { get; set; } = string.Empty;
    private decimal? QuantityInput { get; set; } = 10;
    private VendorRecommendationDto? SelectedVendorForProduct { get; set; }
    private List<VendorRecommendationDto>? VendorRecommendations { get; set; }
    private bool HasActiveContractsForSelectedProduct { get; set; } = false;
    private Dictionary<int, VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto> LatestReviewsByVendorDict { get; set; } = new();
    private string VendorSortBy { get; set; } = "Reviews";

    // Validation & Error Messages
    private string ValidationMessage { get; set; } = string.Empty;
    private string ItemValidationMessage { get; set; } = string.Empty;
    private string CartValidationMessage { get; set; } = string.Empty;
    private string? PurchaseRequestError { get; set; }
    private string? RecommendationsError { get; set; }
    private string? DispatchSuccessMessage { get; set; }

    // Loading & Async Flags
    private bool IsLoadingOutlets { get; set; } = true;
    private bool HasErrorOutlets { get; set; } = false;
    private bool IsLoadingProducts { get; set; } = false;
    private bool HasErrorProducts { get; set; } = false;
    private bool IsFindingVendors { get; set; } = false;
    private bool IsDispatching { get; set; } = false;

    // Navigation & Shell State
    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;
    private bool ShowNotificationDropdown { get; set; } = false;
    private bool ShowProfileModal { get; set; } = false;

    private PurchaseRequestDto? CreatedPurchaseRequest { get; set; }

    // Cart Computed Helpers
    private decimal TotalCartEstimatedValue => CartItems.Sum(i => i.Quantity * i.UnitPrice);
    private List<string> DistinctVendorNamesInCart => CartItems.Select(i => i.VendorName).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList();

    private List<VendorRecommendationDto> SortedVendorRecommendations
    {
        get
        {
            if (VendorRecommendations == null) return new();
            return VendorSortBy switch
            {
                "Price" => VendorRecommendations
                    .OrderBy(r => r.UnitPrice)
                    .ThenBy(r => r.Rank)
                    .ToList(),
                "Reviews" => VendorRecommendations
                    .OrderBy(r => r.Rank)
                    .ThenByDescending(r => r.AverageRating)
                    .ThenByDescending(r => r.TotalFeedbackCount)
                    .ToList(),
                _ => VendorRecommendations.OrderBy(r => r.Rank).ToList()
            };
        }
    }

    private List<VendorRecommendationDto> GetSortedAiRecommendations(List<VendorRecommendationDto>? recs)
    {
        if (recs == null) return new();
        return VendorSortBy switch
        {
            "Price" => recs
                .OrderBy(r => r.UnitPrice)
                .ThenBy(r => r.Rank)
                .ToList(),
            "Reviews" => recs
                .OrderBy(r => r.Rank)
                .ThenByDescending(r => r.AverageRating)
                .ThenByDescending(r => r.TotalFeedbackCount)
                .ToList(),
            _ => recs.OrderBy(r => r.Rank).ToList()
        };
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
        Nav.NavigateTo("/login", true);
    }

    private async Task MarkNotificationRead(int notificationId)
    {
        var notif = Notifications.FirstOrDefault(n => n.NotificationID == notificationId);
        if (notif != null && !notif.IsRead)
        {
            var success = await Api.MarkNotificationReadAsync(notificationId);
            if (success)
            {
                notif.IsRead = true;
                StateHasChanged();
            }
        }
    }

    private async Task MarkAllAsRead()
    {
        var success = await Api.MarkAllNotificationsReadAsync();
        if (success)
        {
            if (Notifications != null)
            {
                foreach (var notif in Notifications)
                {
                    notif.IsRead = true;
                }
            }
            StateHasChanged();
        }
    }

    private async Task ClearAll()
    {
        var success = await Api.ClearAllNotificationsAsync();
        if (success)
        {
            Notifications?.Clear();
            StateHasChanged();
        }
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

            // Resolve Org Name associated with the assigned outlet (consistent with PurchaseRequestDetails)
            var activeOutlet = Outlets.FirstOrDefault(o => o.OutletID == SelectedOutletId);
            string resolvedOrgName = string.Empty;

            // 1. Direct from activeOutlet.OrganizationName
            if (activeOutlet != null && !string.IsNullOrWhiteSpace(activeOutlet.OrganizationName) && !activeOutlet.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
            {
                resolvedOrgName = activeOutlet.OrganizationName;
            }

            // 2. Lookup in allOrgs by activeOutlet.OrganizationID
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && activeOutlet != null && activeOutlet.OrganizationID > 0)
            {
                var matchedOrg = allOrgs.FirstOrDefault(o => o.OrganizationID == activeOutlet.OrganizationID);
                if (matchedOrg != null && !string.IsNullOrWhiteSpace(matchedOrg.OrganizationName) && !matchedOrg.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
                {
                    resolvedOrgName = matchedOrg.OrganizationName;
                }
            }

            // 3. Direct API call GetOrganizationByIdAsync for activeOutlet.OrganizationID
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && activeOutlet != null && activeOutlet.OrganizationID > 0)
            {
                try
                {
                    var directOrg = await Api.GetOrganizationByIdAsync(activeOutlet.OrganizationID);
                    if (directOrg != null && !string.IsNullOrWhiteSpace(directOrg.OrganizationName) && !directOrg.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedOrgName = directOrg.OrganizationName;
                    }
                }
                catch { }
            }

            // 4. Sibling outlet matching the same OrganizationID
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && activeOutlet != null && activeOutlet.OrganizationID > 0)
            {
                var sibling = Outlets.FirstOrDefault(o => o.OrganizationID == activeOutlet.OrganizationID && !string.IsNullOrWhiteSpace(o.OrganizationName) && !o.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase));
                if (sibling != null)
                {
                    resolvedOrgName = sibling.OrganizationName;
                }
            }

            // 5. Invoices associated with this outlet
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && activeOutlet != null)
            {
                try
                {
                    var invoices = await Api.GetInvoicesAsync();
                    var invMatch = invoices?.FirstOrDefault(i => i.OutletID == activeOutlet.OutletID && !string.IsNullOrWhiteSpace(i.OrganizationName) && !i.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase));
                    if (invMatch != null)
                    {
                        resolvedOrgName = invMatch.OrganizationName;
                    }
                }
                catch { }
            }

            // 6. User Auth Organization as fallback if matches
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && Auth.OrganizationID.HasValue && Auth.OrganizationID.Value > 0)
            {
                var authOrg = allOrgs.FirstOrDefault(o => o.OrganizationID == Auth.OrganizationID.Value);
                if (authOrg != null && !string.IsNullOrWhiteSpace(authOrg.OrganizationName) && !authOrg.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
                {
                    resolvedOrgName = authOrg.OrganizationName;
                }
                else
                {
                    try
                    {
                        var directAuthOrg = await Api.GetOrganizationByIdAsync(Auth.OrganizationID.Value);
                        if (directAuthOrg != null && !string.IsNullOrWhiteSpace(directAuthOrg.OrganizationName) && !directAuthOrg.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
                        {
                            resolvedOrgName = directAuthOrg.OrganizationName;
                        }
                    }
                    catch { }
                }
            }

            // 7. First named organization in allOrgs
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && allOrgs.Count > 0)
            {
                var firstNamed = allOrgs.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.OrganizationName) && !o.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase));
                if (firstNamed != null)
                {
                    resolvedOrgName = firstNamed.OrganizationName;
                }
            }

            OrganizationName = !string.IsNullOrWhiteSpace(resolvedOrgName) ? resolvedOrgName : "Organization";

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

    // --- MODE SWITCHING & CART MANAGEMENT ---
    private void StartCreateRequest()
    {
        IsCreateMode = true;
        CurrentStep = 1;
        CartItems.Clear();
        EditingCartItemKey = null;
        SelectedProduct = null;
        QuantityInput = 10;
        SelectedVendorForProduct = null;
        CreatedPurchaseRequest = null;
        VendorRecommendations = null;
        HasActiveContractsForSelectedProduct = false;
        LatestReviewsByVendorDict.Clear();
        ValidationMessage = string.Empty;
        ItemValidationMessage = string.Empty;
        CartValidationMessage = string.Empty;
        PurchaseRequestError = null;
        RecommendationsError = null;
        DispatchSuccessMessage = null;
        SearchProductQuery = string.Empty;
        ProcurementInputMode = "Manual";
        ResetAiInput();
    }

    private void CancelCreateRequest()
    {
        IsCreateMode = false;
        CurrentStep = 1;
        CartItems.Clear();
        EditingCartItemKey = null;
        SelectedProduct = null;
        SelectedVendorForProduct = null;
        CreatedPurchaseRequest = null;
        VendorRecommendations = null;
        HasActiveContractsForSelectedProduct = false;
        LatestReviewsByVendorDict.Clear();
        ValidationMessage = string.Empty;
        ItemValidationMessage = string.Empty;
        CartValidationMessage = string.Empty;
        PurchaseRequestError = null;
        RecommendationsError = null;
        DispatchSuccessMessage = null;
        ProcurementInputMode = "Manual";
        ResetAiInput();
    }

    private async Task GoToStep2AddProducts()
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
        SelectedVendorForProduct = null;
        ItemValidationMessage = string.Empty;
        CartValidationMessage = string.Empty;
        PurchaseRequestError = null;
        RecommendationsError = null;

        await FetchVendorRecommendations();

        if (VendorRecommendations != null && VendorRecommendations.Count > 0 && SelectedVendorForProduct == null)
        {
            SelectedVendorForProduct = VendorRecommendations[0];
        }
    }

    private void SelectVendorForProduct(VendorRecommendationDto vendor)
    {
        SelectedVendorForProduct = vendor;
        ItemValidationMessage = string.Empty;
    }

    private void AddItemToCart()
    {
        ItemValidationMessage = string.Empty;
        CartValidationMessage = string.Empty;

        if (SelectedProduct == null)
        {
            ItemValidationMessage = "Please select a product from the catalog.";
            return;
        }

        if (!QuantityInput.HasValue || QuantityInput.Value <= 0)
        {
            ItemValidationMessage = $"Please enter a valid quantity greater than 0 for {SelectedProduct.ProductName}.";
            return;
        }

        if (SelectedVendorForProduct == null)
        {
            ItemValidationMessage = $"Please select a vendor for {SelectedProduct.ProductName}.";
            return;
        }

        if (!string.IsNullOrEmpty(EditingCartItemKey))
        {
            // Update existing cart item being edited
            var existing = CartItems.FirstOrDefault(i => i.ItemKey == EditingCartItemKey);
            if (existing != null)
            {
                // Check if another cart line already exists with the newly selected product and vendor
                var duplicate = CartItems.FirstOrDefault(i =>
                    i.ItemKey != EditingCartItemKey &&
                    i.ProductID == SelectedProduct.ProductID &&
                    i.VendorID == SelectedVendorForProduct.VendorID);

                if (duplicate != null)
                {
                    // Merge edited quantity into the already existing product + vendor line and remove this edited line
                    duplicate.Quantity += QuantityInput.Value;
                    duplicate.UnitPrice = SelectedVendorForProduct.UnitPrice;
                    duplicate.EstimatedDeliveryDays = SelectedVendorForProduct.EstimatedDeliveryDays;
                    duplicate.AverageRating = SelectedVendorForProduct.AverageRating;
                    duplicate.TotalFeedbackCount = SelectedVendorForProduct.TotalFeedbackCount;
                    duplicate.AverageQualityRating = SelectedVendorForProduct.AverageQualityRating;
                    duplicate.SmartBadge = SelectedVendorForProduct.SmartBadge;
                    duplicate.HasActiveContract = SelectedVendorForProduct.HasActiveContract;
                    duplicate.RemainingQuantity = SelectedVendorForProduct.RemainingQuantity;
                    CartItems.Remove(existing);
                }
                else
                {
                    existing.ProductID = SelectedProduct.ProductID;
                    existing.ProductName = SelectedProduct.ProductName;
                    existing.Category = SelectedProduct.Category ?? "General";
                    existing.Quantity = QuantityInput.Value;
                    existing.Unit = SelectedProduct.Unit;
                    existing.VendorID = SelectedVendorForProduct.VendorID;
                    existing.VendorName = SelectedVendorForProduct.VendorName;
                    existing.UnitPrice = SelectedVendorForProduct.UnitPrice;
                    existing.EstimatedDeliveryDays = SelectedVendorForProduct.EstimatedDeliveryDays;
                    existing.AverageRating = SelectedVendorForProduct.AverageRating;
                    existing.TotalFeedbackCount = SelectedVendorForProduct.TotalFeedbackCount;
                    existing.AverageQualityRating = SelectedVendorForProduct.AverageQualityRating;
                    existing.SmartBadge = SelectedVendorForProduct.SmartBadge;
                    existing.HasActiveContract = SelectedVendorForProduct.HasActiveContract;
                    existing.RemainingQuantity = SelectedVendorForProduct.RemainingQuantity;
                }
            }
            EditingCartItemKey = null;
        }
        else
        {
            // Check if product with the same vendor already exists in cart
            var existing = CartItems.FirstOrDefault(i =>
                i.ProductID == SelectedProduct.ProductID &&
                i.VendorID == SelectedVendorForProduct.VendorID);

            if (existing != null)
            {
                // Increase quantity for the same product and vendor, and refresh latest rate/contract info
                existing.Quantity += QuantityInput.Value;
                existing.Unit = SelectedProduct.Unit;
                existing.UnitPrice = SelectedVendorForProduct.UnitPrice;
                existing.EstimatedDeliveryDays = SelectedVendorForProduct.EstimatedDeliveryDays;
                existing.AverageRating = SelectedVendorForProduct.AverageRating;
                existing.TotalFeedbackCount = SelectedVendorForProduct.TotalFeedbackCount;
                existing.AverageQualityRating = SelectedVendorForProduct.AverageQualityRating;
                existing.SmartBadge = SelectedVendorForProduct.SmartBadge;
                existing.HasActiveContract = SelectedVendorForProduct.HasActiveContract;
                existing.RemainingQuantity = SelectedVendorForProduct.RemainingQuantity;
            }
            else
            {
                // Add new item to cart as a separate cart line for this vendor
                CartItems.Add(new ProcurementItem
                {
                    ProductID = SelectedProduct.ProductID,
                    ProductName = SelectedProduct.ProductName,
                    Category = SelectedProduct.Category ?? "General",
                    Quantity = QuantityInput.Value,
                    Unit = SelectedProduct.Unit,
                    VendorID = SelectedVendorForProduct.VendorID,
                    VendorName = SelectedVendorForProduct.VendorName,
                    UnitPrice = SelectedVendorForProduct.UnitPrice,
                    EstimatedDeliveryDays = SelectedVendorForProduct.EstimatedDeliveryDays,
                    AverageRating = SelectedVendorForProduct.AverageRating,
                    TotalFeedbackCount = SelectedVendorForProduct.TotalFeedbackCount,
                    AverageQualityRating = SelectedVendorForProduct.AverageQualityRating,
                    SmartBadge = SelectedVendorForProduct.SmartBadge,
                    HasActiveContract = SelectedVendorForProduct.HasActiveContract,
                    RemainingQuantity = SelectedVendorForProduct.RemainingQuantity
                });
            }
        }

        // Reset product picker for adding subsequent items
        ResetProductPicker();
    }

    private async Task EditCartItem(ProcurementItem item)
    {
        EditingCartItemKey = item.ItemKey;
        var matched = AllProducts.FirstOrDefault(p => p.ProductID == item.ProductID);
        SelectedProduct = matched ?? new ProductDto
        {
            ProductID = item.ProductID,
            ProductName = item.ProductName,
            Category = item.Category,
            Unit = item.Unit
        };
        QuantityInput = item.Quantity;
        ItemValidationMessage = string.Empty;

        await FetchVendorRecommendations();

        if (VendorRecommendations != null && VendorRecommendations.Count > 0)
        {
            SelectedVendorForProduct = VendorRecommendations.FirstOrDefault(v => v.VendorID == item.VendorID) ?? VendorRecommendations[0];
        }
    }

    private void CancelEditCartItem()
    {
        EditingCartItemKey = null;
        ResetProductPicker();
    }

    private void RemoveCartItem(string itemKey)
    {
        CartItems.RemoveAll(i => i.ItemKey == itemKey);
        if (EditingCartItemKey == itemKey)
        {
            CancelEditCartItem();
        }
        CartValidationMessage = string.Empty;
    }

    private void UpdateCartItemQuantity(ProcurementItem item, decimal newQuantity)
    {
        if (newQuantity > 0)
        {
            item.Quantity = newQuantity;
        }
    }

    private void ResetProductPicker()
    {
        SelectedProduct = null;
        SelectedVendorForProduct = null;
        QuantityInput = 10;
        VendorRecommendations = null;
        HasActiveContractsForSelectedProduct = false;
        SearchProductQuery = string.Empty;
        ItemValidationMessage = string.Empty;
        EditingCartItemKey = null;
    }

    private void GoToStepReview()
    {
        CartValidationMessage = string.Empty;
        PurchaseRequestError = null;

        // Auto-add current valid selection if cart is currently empty
        if (CartItems.Count == 0 && SelectedProduct != null && SelectedVendorForProduct != null && QuantityInput.HasValue && QuantityInput.Value > 0)
        {
            AddItemToCart();
        }

        if (CartItems.Count == 0)
        {
            CartValidationMessage = "Please add at least one product with an assigned vendor to your procurement request.";
            return;
        }

        // Validate all cart items
        foreach (var item in CartItems)
        {
            if (item.Quantity <= 0)
            {
                CartValidationMessage = $"Please enter a valid quantity greater than 0 for {item.ProductName}.";
                return;
            }
            if (item.VendorID <= 0)
            {
                CartValidationMessage = $"Please select a vendor for {item.ProductName}.";
                return;
            }
        }

        CurrentStep = 3;
    }

    private void GoBackToStep(int step)
    {
        CurrentStep = step;
        PurchaseRequestError = null;
        CartValidationMessage = string.Empty;
        ItemValidationMessage = string.Empty;
    }

    private async Task LoadLatestReviewsForVendorsAsync(List<VendorRecommendationDto> recs, int productId)
    {
        if (recs == null || recs.Count == 0) return;
        var distinctVendorIds = recs.Select(r => r.VendorID).Distinct().ToList();
        var reviewTasks = distinctVendorIds.ToDictionary(
            vId => vId,
            vId => Api.GetVendorReviewsAsync(vId, productId)
        );
        try
        {
            await Task.WhenAll(reviewTasks.Values);
            foreach (var kvp in reviewTasks)
            {
                var list = await kvp.Value;
                if (list != null && list.Count > 0)
                {
                    LatestReviewsByVendorDict[kvp.Key] = list[0];
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Procurement] Error preloading reviews: {ex.Message}");
        }
    }

    private async Task FetchVendorRecommendations()
    {
        if (SelectedProduct == null || SelectedOutletId <= 0) return;

        IsFindingVendors = true;
        RecommendationsError = null;
        HasActiveContractsForSelectedProduct = false;
        StateHasChanged();

        try
        {
            var response = await Api.GetRecommendationsForProductAsync(SelectedProduct.ProductID, SelectedOutletId);

            if (response != null && response.Recommendations != null)
            {
                HasActiveContractsForSelectedProduct = response.HasActiveContracts;
                VendorRecommendations = response.Recommendations;

                if (VendorRecommendations.Count > 0 && SelectedVendorForProduct == null)
                {
                    SelectedVendorForProduct = VendorRecommendations[0];
                }

                await LoadLatestReviewsForVendorsAsync(VendorRecommendations, SelectedProduct.ProductID);
            }
            else
            {
                VendorRecommendations = new List<VendorRecommendationDto>();
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

    // --- AI PROCUREMENT ASSISTANT METHODS ---

    private void SetInputMode(string mode)
    {
        ProcurementInputMode = mode;
        StateHasChanged();
    }

    private void SetVoiceLanguage(string lang)
    {
        SelectedVoiceLanguage = lang;
        StateHasChanged();
    }

    private void ToggleTts()
    {
        IsTtsEnabled = !IsTtsEnabled;
        if (!IsTtsEnabled)
        {
            _ = JS.InvokeVoidAsync("voiceSynthesis.stop");
        }
        StateHasChanged();
    }

    private async Task SpeakAssistantMessageAsync(string? text, string lang)
    {
        if (string.IsNullOrWhiteSpace(text) || !IsTtsEnabled) return;
        try
        {
            await JS.InvokeVoidAsync("voiceSynthesis.speak", text, lang);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Procurement] TTS speak error: {ex.Message}");
        }
    }

    private async Task ReplayAssistantSpeech()
    {
        if (!string.IsNullOrWhiteSpace(AssistantSpeechMessage))
        {
            bool isHindi = IsHindiPrompt(AssistantSpeechMessage) || SelectedVoiceLanguage == "hi-IN";
            await SpeakAssistantMessageAsync(AssistantSpeechMessage, isHindi ? "hi-IN" : "en-US");
        }
    }

    private static bool IsHindiPrompt(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u0900-\u097F]")) return true;
        var hinglishKeywords = new[] { "mujhe", "chahiye", "kilo", "taaza", "tamatar", "aloo", "pyaaz", "santre", "kaun", "hai", "sabse", "sasta", "jaldi", "aur", "ke liye", "pehle", "pehla", "dusra", "batao", "kyun", "fresh" };
        string lower = text.ToLowerInvariant();
        return hinglishKeywords.Any(k => System.Text.RegularExpressions.Regex.IsMatch(lower, $@"\b{System.Text.RegularExpressions.Regex.Escape(k)}\b"));
    }

    private void ResetAiInput()
    {
        AiPromptInput = string.Empty;
        IsAiParsing = false;
        AiErrorMessage = null;
        AiResponse = null;
        AssistantSpeechMessage = null;
        AiItemRecommendations.Clear();
        AiItemHasActiveContracts.Clear();
        AiItemLoadingStates.Clear();
        AiItemErrorStates.Clear();
        AiSelectedVendors.Clear();
        AiItemAddedToCartKeys.Clear();
        IsVoiceListening = false;
        VoiceMessage = null;
        _ = JS.InvokeVoidAsync("voiceSynthesis.stop");
    }

    private async Task SubmitAiRequestAsync()
    {
        if (string.IsNullOrWhiteSpace(AiPromptInput)) return;

        string prompt = AiPromptInput.Trim();
        bool isHindi = IsHindiPrompt(prompt) || SelectedVoiceLanguage == "hi-IN";

        // Check if this is a follow-up question regarding active recommendations
        if (AiItemRecommendations.Count > 0 && AiItemRecommendations.Values.Any(v => v != null && v.Count > 0))
        {
            if (TryHandleFollowUpQuestion(prompt, isHindi))
            {
                return;
            }
        }

        IsAiParsing = true;
        AiErrorMessage = null;
        AiResponse = null;
        AssistantSpeechMessage = null;
        AiItemRecommendations.Clear();
        AiItemLoadingStates.Clear();
        AiItemErrorStates.Clear();
        AiSelectedVendors.Clear();
        AiItemAddedToCartKeys.Clear();
        StateHasChanged();

        try
        {
            AiResponse = await Api.ParseVoiceProcurementOrderAsync(prompt);

            if (AiResponse == null)
            {
                AiErrorMessage = isHindi
                    ? "एआई सेवा से कनेक्ट करने में असमर्थ। कृपया पुनः प्रयास करें।"
                    : "Unable to connect to AI parsing service. Please try again or use manual product search.";
                return;
            }

            if (!AiResponse.Success && string.IsNullOrWhiteSpace(AiResponse.OutletResolutionStatus))
            {
                AiErrorMessage = !string.IsNullOrWhiteSpace(AiResponse.Message)
                    ? AiResponse.Message
                    : (isHindi ? "अनुरोध को समझने में असमर्थ। कृपया उत्पाद और मात्रा स्पष्ट रूप से बताएं।" : "Unable to parse request. Please describe the items and quantity clearly.");
                AssistantSpeechMessage = AiErrorMessage;
                if (IsTtsEnabled) await SpeakAssistantMessageAsync(AssistantSpeechMessage, isHindi ? "hi-IN" : "en-US");
                return;
            }

            // Check Outlet Resolution
            bool isOutletValid = string.Equals(AiResponse.OutletResolutionStatus, "Resolved", StringComparison.OrdinalIgnoreCase) ||
                                 (string.Equals(AiResponse.OutletResolutionStatus, "NotSpecified", StringComparison.OrdinalIgnoreCase) && SelectedOutletId > 0);

            if (!isOutletValid)
            {
                return;
            }

            int targetOutletId = AiResponse.OutletID.HasValue && AiResponse.OutletID.Value > 0
                ? AiResponse.OutletID.Value
                : SelectedOutletId;

            if (AllProducts.Count == 0)
            {
                await LoadProductsAsync();
            }

            // Evaluate each resolved item using contract-aware backend endpoint
            for (int i = 0; i < AiResponse.Items.Count; i++)
            {
                var item = AiResponse.Items[i];
                int itemIdx = i;

                // CRITICAL: NEVER PRESELECT A VENDOR!
                AiSelectedVendors[itemIdx] = null;

                if (string.Equals(item.ResolutionStatus, "Resolved", StringComparison.OrdinalIgnoreCase) && item.ProductID.HasValue)
                {
                    await LoadRecommendationsForAiItemAsync(itemIdx, item);
                }
            }

            // Construct assistant acknowledgment message & play speech
            int totalVendors = AiItemRecommendations.Values.Sum(v => v?.Count ?? 0);
            if (AiResponse.Items.All(i => i.ResolutionStatus == "Resolved"))
            {
                string baseText = !string.IsNullOrWhiteSpace(AiResponse.AssistantResponseText)
                    ? AiResponse.AssistantResponseText
                    : (isHindi ? "ठीक है। मैंने आपके आदेश की आवश्यकता समझ ली है।" : "Understood your procurement requirement.");

                AssistantSpeechMessage = isHindi
                    ? $"{baseText} {totalVendors} विक्रेता उपलब्ध हैं।"
                    : $"{baseText} {totalVendors} suppliers available.";
            }
            else if (AiResponse.Items.Any(i => i.ResolutionStatus == "Ambiguous"))
            {
                var ambItem = AiResponse.Items.First(i => i.ResolutionStatus == "Ambiguous");
                string choices = ambItem.AmbiguousMatches != null && ambItem.AmbiguousMatches.Count > 0
                    ? string.Join(isHindi ? " या " : " or ", ambItem.AmbiguousMatches)
                    : ambItem.ProductName ?? ambItem.SpokenProductName;
                AssistantSpeechMessage = isHindi
                    ? $"कृपया बताएं कि आपको {choices} चाहिए।"
                    : $"Please clarify whether you need {choices}.";
            }
            else if (AiResponse.Items.Any(i => i.ResolutionStatus == "NotFound"))
            {
                var nfItem = AiResponse.Items.First(i => i.ResolutionStatus == "NotFound");
                AssistantSpeechMessage = isHindi
                    ? $"मुझे '{nfItem.SpokenProductName}' का मिलान आपके कैटलॉग में नहीं मिला।"
                    : $"Product '{nfItem.SpokenProductName}' was not found in active catalog.";
            }

            if (!string.IsNullOrWhiteSpace(AssistantSpeechMessage) && IsTtsEnabled)
            {
                await SpeakAssistantMessageAsync(AssistantSpeechMessage, isHindi ? "hi-IN" : "en-US");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Procurement] AI submit error: {ex.Message}");
            AiErrorMessage = "An unexpected error occurred while processing your request.";
        }
        finally
        {
            IsAiParsing = false;
            StateHasChanged();
        }
    }

    private bool TryHandleFollowUpQuestion(string prompt, bool isHindi)
    {
        string pLower = prompt.ToLowerInvariant();

        // Check if query has a numeric quantity + product syntax indicating a NEW procurement request
        bool hasProcurementIntent = System.Text.RegularExpressions.Regex.IsMatch(pLower, @"\b\d+(?:\.\d+)?\s*(?:kg|kgs|किलो|किग्रा|gm|ग्राम|ltr|लीटर|box|डिब्बा|पैकेट|packet|unit|units|pcs)\b");
        if (hasProcurementIntent) return false;

        var allRecs = AiItemRecommendations.Values
            .Where(list => list != null)
            .SelectMany(list => list)
            .ToList();

        if (allRecs.Count == 0) return false;

        // 1. Cheapest / Best Price question
        bool isCheapestQuery = pLower.Contains("सस्ता") || pLower.Contains("सस्ते") || pLower.Contains("कम कीमत") ||
                               pLower.Contains("कम दाम") || pLower.Contains("cheapest") || pLower.Contains("lowest price") ||
                               pLower.Contains("best price") || pLower.Contains("sabse sasta") || pLower.Contains("cheap");

        if (isCheapestQuery)
        {
            var bestPrice = allRecs.OrderBy(r => r.UnitPrice).First();
            string prodUnit = AllProducts.FirstOrDefault(p => p.ProductID == bestPrice.ProductID)?.Unit ?? "kg";
            string unit = isHindi ? (prodUnit.Equals("kg", StringComparison.OrdinalIgnoreCase) ? "किलो" : prodUnit) : prodUnit;
            AssistantSpeechMessage = isHindi
                ? $"सबसे कम यूनिट कीमत वाला विक्रेता {bestPrice.VendorName} है, जिसकी कीमत ₹{bestPrice.UnitPrice:N0} प्रति {unit} है।"
                : $"The vendor with the lowest unit price is {bestPrice.VendorName} at ₹{bestPrice.UnitPrice:N0} per {unit}.";

            if (IsTtsEnabled) _ = SpeakAssistantMessageAsync(AssistantSpeechMessage, isHindi ? "hi-IN" : "en-US");
            StateHasChanged();
            return true;
        }

        // 2. Fastest delivery question
        bool isFastestQuery = pLower.Contains("जल्दी") || pLower.Contains("कम समय") || pLower.Contains("fastest") ||
                              pLower.Contains("quickest") || pLower.Contains("earliest") || pLower.Contains("sabse jaldi") ||
                              pLower.Contains("fast delivery");

        if (isFastestQuery)
        {
            var fastest = allRecs.OrderBy(r => r.EstimatedDeliveryDays).First();
            AssistantSpeechMessage = isHindi
                ? $"सबसे जल्दी डिलीवरी देने वाला विक्रेता {fastest.VendorName} है, जो {fastest.EstimatedDeliveryDays} दिनों में डिलीवरी करता है।"
                : $"The fastest delivering vendor is {fastest.VendorName}, delivering in approximately {fastest.EstimatedDeliveryDays} days.";

            if (IsTtsEnabled) _ = SpeakAssistantMessageAsync(AssistantSpeechMessage, isHindi ? "hi-IN" : "en-US");
            StateHasChanged();
            return true;
        }

        // 3. First / Top vendor inquiry
        bool isFirstVendorQuery = pLower.Contains("पहला") || pLower.Contains("pahla") || pLower.Contains("first vendor") ||
                                  pLower.Contains("top vendor") || pLower.Contains("number 1") || pLower.Contains("no 1");

        if (isFirstVendorQuery)
        {
            var top = allRecs.OrderByDescending(r => r.OverallScore).First();
            string contractNotice = top.HasActiveContract ? (isHindi ? " साथ ही इसके पास सक्रिय अनुबंध भी है।" : " It also holds an active contract.") : "";
            AssistantSpeechMessage = isHindi
                ? $"पहला विक्रेता {top.VendorName} सबसे बेहतर है क्योंकि इसका समग्र स्कोर {top.OverallScore:0.#}/100 है, औसत रेटिंग {top.AverageRating:0.0}★ है और कीमत ₹{top.UnitPrice:N0} है।{contractNotice}"
                : $"The top-ranked vendor is {top.VendorName} with an overall score of {top.OverallScore:0.#}/100, average rating of {top.AverageRating:0.0}★, and price of ₹{top.UnitPrice:N0}.{contractNotice}";

            if (IsTtsEnabled) _ = SpeakAssistantMessageAsync(AssistantSpeechMessage, isHindi ? "hi-IN" : "en-US");
            StateHasChanged();
            return true;
        }

        // 4. Second vendor inquiry
        bool isSecondVendorQuery = pLower.Contains("दूसरा") || pLower.Contains("dusra") || pLower.Contains("second vendor") ||
                                   pLower.Contains("second") || pLower.Contains("number 2") || pLower.Contains("no 2");

        if (isSecondVendorQuery)
        {
            var second = allRecs.OrderByDescending(r => r.OverallScore).Skip(1).FirstOrDefault() ?? allRecs.First();
            AssistantSpeechMessage = isHindi
                ? $"दूसरा विक्रेता {second.VendorName} है, जिसका समग्र स्कोर {second.OverallScore:0.#}/100, रेटिंग {second.AverageRating:0.0}★, डिलीवरी समय {second.EstimatedDeliveryDays} दिन और कीमत ₹{second.UnitPrice:N0} है।"
                : $"The second vendor is {second.VendorName} with an overall score of {second.OverallScore:0.#}/100, rating of {second.AverageRating:0.0}★, delivery in {second.EstimatedDeliveryDays} days, and price of ₹{second.UnitPrice:N0}.";

            if (IsTtsEnabled) _ = SpeakAssistantMessageAsync(AssistantSpeechMessage, isHindi ? "hi-IN" : "en-US");
            StateHasChanged();
            return true;
        }

        return false;
    }

    private async Task LoadRecommendationsForAiItemAsync(int itemIndex, ParsedProcurementItemDto item)
    {
        if (!item.ProductID.HasValue || item.ProductID.Value <= 0) return;

        int targetOutletId = (AiResponse?.OutletID.HasValue == true && AiResponse.OutletID.Value > 0)
            ? AiResponse.OutletID.Value
            : SelectedOutletId;

        AiItemLoadingStates[itemIndex] = true;
        AiSelectedVendors[itemIndex] = null; // CRITICAL: NEVER PRESELECT!
        AiItemHasActiveContracts[itemIndex] = false;
        StateHasChanged();

        try
        {
            var response = await Api.GetRecommendationsForProductAsync(item.ProductID.Value, targetOutletId);
            if (response != null && response.Recommendations != null)
            {
                AiItemHasActiveContracts[itemIndex] = response.HasActiveContracts;
                AiItemRecommendations[itemIndex] = response.Recommendations;
                await LoadLatestReviewsForVendorsAsync(response.Recommendations, item.ProductID.Value);
            }
            else
            {
                AiItemRecommendations[itemIndex] = new List<VendorRecommendationDto>();
            }
            AiItemErrorStates[itemIndex] = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Procurement] Error loading recommendations for item {item.ProductID}: {ex.Message}");
            AiItemRecommendations[itemIndex] = new();
            AiItemErrorStates[itemIndex] = "Unable to load vendor recommendations for this product.";
        }
        finally
        {
            AiItemLoadingStates[itemIndex] = false;
            StateHasChanged();
        }
    }

    private void ToggleVendorForAiItem(ParsedProcurementItemDto item, VendorRecommendationDto vendorRec)
    {
        if (!item.ProductID.HasValue || item.ProductID.Value <= 0 || vendorRec == null || vendorRec.VendorID <= 0) return;

        var existing = CartItems.FirstOrDefault(c =>
            c.ProductID == item.ProductID.Value &&
            c.VendorID == vendorRec.VendorID);

        if (existing != null)
        {
            // Toggle off: remove this specific vendor for this product from cart
            CartItems.Remove(existing);
            if (EditingCartItemKey == existing.ItemKey)
            {
                CancelEditCartItem();
            }
        }
        else
        {
            // Add new vendor-product combination to cart
            string category = "General";
            var prod = AllProducts.FirstOrDefault(p => p.ProductID == item.ProductID.Value);
            if (prod != null && !string.IsNullOrWhiteSpace(prod.Category))
            {
                category = prod.Category;
            }

            decimal qty = item.Quantity > 0 ? item.Quantity : 10;
            string unit = !string.IsNullOrWhiteSpace(item.Unit) ? item.Unit : (prod?.Unit ?? "units");

            CartItems.Add(new ProcurementItem
            {
                ProductID = item.ProductID.Value,
                ProductName = item.ProductName ?? item.SpokenProductName ?? (prod?.ProductName ?? "Item"),
                Category = category,
                Quantity = qty,
                Unit = unit,
                VendorID = vendorRec.VendorID,
                VendorName = vendorRec.VendorName,
                UnitPrice = vendorRec.UnitPrice,
                EstimatedDeliveryDays = vendorRec.EstimatedDeliveryDays,
                AverageRating = vendorRec.AverageRating,
                TotalFeedbackCount = vendorRec.TotalFeedbackCount,
                AverageQualityRating = vendorRec.AverageQualityRating,
                SmartBadge = vendorRec.SmartBadge,
                HasActiveContract = vendorRec.HasActiveContract,
                RemainingQuantity = vendorRec.RemainingQuantity
            });
        }

        CartValidationMessage = string.Empty;
        StateHasChanged();
    }

    private async Task SelectAmbiguousMatchAsync(int itemIndex, ParsedProcurementItemDto item, string chosenMatchName)
    {
        if (AllProducts.Count == 0)
        {
            await LoadProductsAsync();
        }

        var matched = AllProducts.FirstOrDefault(p =>
            string.Equals(p.ProductName.Trim(), chosenMatchName.Trim(), StringComparison.OrdinalIgnoreCase));

        if (matched == null)
        {
            matched = AllProducts.FirstOrDefault(p =>
                p.ProductName.Contains(chosenMatchName.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (matched != null)
        {
            item.ProductID = matched.ProductID;
            item.ProductName = matched.ProductName;
            item.Unit = matched.Unit;
            item.ResolutionStatus = "Resolved";

            await LoadRecommendationsForAiItemAsync(itemIndex, item);

            bool isHindi = SelectedVoiceLanguage == "hi-IN" || IsHindiPrompt(AiPromptInput);
            int vendorCount = AiItemRecommendations.TryGetValue(itemIndex, out var recs) ? recs.Count : 0;
            AssistantSpeechMessage = isHindi
                ? $"ठीक है। मैंने {matched.ProductName} का चयन किया है। {vendorCount} विक्रेता उपलब्ध हैं।"
                : $"Selected {matched.ProductName}. {vendorCount} suppliers available.";

            if (IsTtsEnabled)
            {
                _ = SpeakAssistantMessageAsync(AssistantSpeechMessage, isHindi ? "hi-IN" : "en-US");
            }
        }
        else
        {
            item.ResolutionStatus = "NotFound";
            item.Message = $"Product '{chosenMatchName}' not found in active catalog.";
            StateHasChanged();
        }
    }

    private async Task UpdateAiItemQuantity(int itemIndex, ParsedProcurementItemDto item, decimal newQty)
    {
        if (newQty > 0)
        {
            item.Quantity = newQty;
            if (item.ProductID.HasValue)
            {
                var matchingCartItems = CartItems.Where(i => i.ProductID == item.ProductID.Value).ToList();
                foreach (var cartItem in matchingCartItems)
                {
                    cartItem.Quantity = newQty;
                }
            }
            if (item.ResolutionStatus == "InvalidQuantity" && item.ProductID.HasValue)
            {
                item.ResolutionStatus = "Resolved";
                await LoadRecommendationsForAiItemAsync(itemIndex, item);
            }
            StateHasChanged();
        }
    }

    //  VOICE INPUT METHODS 

    private async Task StartVoiceInput()
    {
        try
        {
            _dotNetRef ??= DotNetObjectReference.Create(this);
            VoiceMessage = null;
            await JS.InvokeVoidAsync("voiceRecognition.start", _dotNetRef, SelectedVoiceLanguage);
        }
        catch (Exception)
        {
            VoiceMessage = "Voice input is not supported in this browser. Please type your requirement.";
            IsVoiceListening = false;
            StateHasChanged();
        }
    }

    private async Task StopVoiceInput()
    {
        try
        {
            await JS.InvokeVoidAsync("voiceRecognition.stop");
        }
        catch { }
        finally
        {
            IsVoiceListening = false;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public void OnVoiceStarted()
    {
        IsVoiceListening = true;
        VoiceMessage = SelectedVoiceLanguage == "hi-IN"
            ? "सुन रहा हूँ... अपनी खरीद आवश्यकता बोलें..."
            : "Listening for procurement requirement...";
        StateHasChanged();
    }

    [JSInvokable]
    public void OnVoiceResult(string transcript)
    {
        IsVoiceListening = false;
        VoiceMessage = null;
        if (!string.IsNullOrWhiteSpace(transcript))
        {
            AiPromptInput = transcript;
            if (System.Text.RegularExpressions.Regex.IsMatch(transcript, @"[\u0900-\u097F]"))
            {
                SelectedVoiceLanguage = "hi-IN";
            }
        }
        StateHasChanged();
    }

    [JSInvokable]
    public void OnVoiceError(string error)
    {
        IsVoiceListening = false;
        VoiceMessage = $"Voice notice: {error}";
        StateHasChanged();
    }

    [JSInvokable]
    public void OnVoiceEnded()
    {
        IsVoiceListening = false;
        StateHasChanged();
    }

    public void Dispose()
    {
        _dotNetRef?.Dispose();
    }
    private async Task CreateAndDispatchPurchaseRequest()
    {
        if (!Auth.IsPurchaseManager && !Auth.IsAdmin) return;
        if (CartItems.Count == 0 || SelectedOutletId <= 0) return;

        // Validate all cart items before submitting
        foreach (var item in CartItems)
        {
            if (item.Quantity <= 0)
            {
                PurchaseRequestError = $"Invalid quantity for {item.ProductName}. Must be greater than 0.";
                return;
            }
            if (item.VendorID <= 0)
            {
                PurchaseRequestError = $"Please select a vendor for {item.ProductName}.";
                return;
            }
        }

        IsDispatching = true;
        PurchaseRequestError = null;
        DispatchSuccessMessage = null;
        StateHasChanged();

        try
        {
            // Build CreatePurchaseRequestCommand with item-level VendorID
            var command = new CreatePurchaseRequestCommand
            {
                OutletID = SelectedOutletId,
                CreatedByUserID = Auth.UserID,
                RequestDate = DateTime.Now,
                Items = CartItems.Select(i => new CreatePurchaseRequestItemDto
                {
                    ProductID = i.ProductID,
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    VendorID = i.VendorID
                }).ToList()
            };

            CreatedPurchaseRequest = await Api.CreatePurchaseRequestAsync(command);
            if (CreatedPurchaseRequest == null)
            {
                PurchaseRequestError = "Unable to create Purchase Request. Please try again.";
                IsDispatching = false;
                StateHasChanged();
                return;
            }

            // Resolve RequestItemIDs for dispatch
            List<PurchaseRequestItemDto> serverItems = CreatedPurchaseRequest.Items ?? new();
            if (serverItems.Count == 0 || serverItems.Any(si => si.RequestItemID <= 0))
            {
                var detailedPr = await Api.GetPurchaseRequestByIdAsync(CreatedPurchaseRequest.RequestID);
                if (detailedPr?.Items != null && detailedPr.Items.Count > 0)
                {
                    serverItems = detailedPr.Items;
                }
            }

            // Map each cart item to its corresponding RequestItemID, ProductID, and VendorID
            var assignments = new List<ItemVendorAssignmentDto>();
            var assignedItemIds = new HashSet<int>();

            for (int i = 0; i < CartItems.Count; i++)
            {
                var cartItem = CartItems[i];
                PurchaseRequestItemDto? matchedServerItem = null;

                // 1. Direct positional check (server preserves insertion sequence)
                if (serverItems.Count > i && serverItems[i].ProductID == cartItem.ProductID && !assignedItemIds.Contains(serverItems[i].RequestItemID))
                {
                    if (serverItems[i].Quantity == cartItem.Quantity)
                    {
                        matchedServerItem = serverItems[i];
                    }
                }

                // 2. Match by ProductID + Quantity not yet assigned
                if (matchedServerItem == null)
                {
                    matchedServerItem = serverItems.FirstOrDefault(si =>
                        si.ProductID == cartItem.ProductID &&
                        si.Quantity == cartItem.Quantity &&
                        !assignedItemIds.Contains(si.RequestItemID));
                }

                // 3. Match by ProductID not yet assigned
                if (matchedServerItem == null)
                {
                    matchedServerItem = serverItems.FirstOrDefault(si =>
                        si.ProductID == cartItem.ProductID &&
                        !assignedItemIds.Contains(si.RequestItemID));
                }

                int requestItemId = matchedServerItem != null && matchedServerItem.RequestItemID > 0
                    ? matchedServerItem.RequestItemID
                    : (serverItems.Count > i ? serverItems[i].RequestItemID : 0);

                if (requestItemId > 0)
                {
                    assignedItemIds.Add(requestItemId);
                }

                assignments.Add(new ItemVendorAssignmentDto
                {
                    RequestItemID = requestItemId,
                    ProductID = cartItem.ProductID,
                    VendorID = cartItem.VendorID
                });
            }
            var result = await Api.DispatchPurchaseRequestAsync(CreatedPurchaseRequest.RequestID, assignments);

            if (result != null && result.Success)
            {
                var distinctVendors = CartItems.Select(c => c.VendorName).Distinct().ToList();
                string vendorNames = string.Join(", ", distinctVendors.Select(v => $"'{v}'"));
                DispatchSuccessMessage = $"Purchase Request PR-{CreatedPurchaseRequest.RequestID} ({CartItems.Count} item{(CartItems.Count > 1 ? "s" : "")}) has been successfully created and dispatched to {vendorNames}.";

                // Refresh the local purchase requests list in background
                _ = Task.Run(async () =>
                {
                    try
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
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Procurement] Background PR list refresh error: {ex.Message}");
                    }
                });
            }
            else
            {
                PurchaseRequestError = result?.Message ?? "Unable to dispatch Purchase Request to assigned vendor(s).";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Procurement] Dispatch error: {ex.Message}");
            PurchaseRequestError = "An error occurred while creating and dispatching the Purchase Request.";
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
            if (pr.Items.Count == 1)
            {
                var first = pr.Items[0];
                return $"{first.Quantity:G29} {first.Unit}".Trim();
            }
            return $"{pr.Items.Count} Items";
        }
        return "â€”";
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
            "rejected" or "declined" or "cancelled" => "status-badge-rejected",
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

    private string ResolveReviewOutletName(VenodorManagementFrontend.Models.VendorPerformance.VendorReviewDto? rev)
    {
        if (rev == null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(rev.OutletName) && !rev.OutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase))
            return rev.OutletName;

        if (rev.OutletID > 0)
        {
            var matched = Outlets.FirstOrDefault(o => o.OutletID == rev.OutletID);
            if (matched != null && !string.IsNullOrWhiteSpace(matched.OutletName))
                return GetDisplayOutletName(matched);
        }

        if (!string.IsNullOrWhiteSpace(ConfirmedOutletName) && !ConfirmedOutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase))
            return ConfirmedOutletName;

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
