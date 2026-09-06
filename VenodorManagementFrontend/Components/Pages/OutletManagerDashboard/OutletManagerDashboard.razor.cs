using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.OutletManagerDashboard;

public partial class OutletManagerDashboard : ComponentBase
{
    [SupplyParameterFromQuery(Name = "tab")]
    public string? QueryTab { get; set; }

    [SupplyParameterFromQuery(Name = "poId")]
    public int? QueryPoId { get; set; }

    private string ActiveTab { get; set; } = "Dashboard";
    private bool IsLoading { get; set; } = true;

    private int UserOutletId { get; set; }
    private int UserOrgId { get; set; }
    private string OutletManagerName { get; set; } = string.Empty;
    private string OutletNameDisplay { get; set; } = string.Empty;
    private string OrganizationNameDisplay { get; set; } = string.Empty;
    private string AssignedOutletAddress { get; set; } = string.Empty;

    private List<PurchaseRequestDto> PRsForMyOutlet { get; set; } = new();
    private List<ContractDto> ContractsForMyOutlet { get; set; } = new();
    private List<PurchaseOrderDto> OrdersForMyOutlet { get; set; } = new();
    private Dictionary<int, ProductDto> ProductLookup { get; set; } = new();
    private PurchaseOrderDto? SelectedPurchaseOrder { get; set; }
    private bool ShowPoDetailsModal { get; set; } = false;
    private string PoErrorMessage { get; set; } = string.Empty;

    private string GetProductName(int productId)
    {
        return ProductLookup.TryGetValue(productId, out var p) ? p.ProductName : $"Product #{productId}";
    }

    private string GetProductUnit(int productId)
    {
        return ProductLookup.TryGetValue(productId, out var p) && !string.IsNullOrWhiteSpace(p.Unit) ? p.Unit : "units";
    }

    private string GetPrimaryProductName(PurchaseOrderDto? po)
    {
        if (po?.Items == null || po.Items.Count == 0) return "General Goods";
        var firstItem = po.Items.First();
        return GetProductName(firstItem.ProductID);
    }

    private string GetPrimaryProductQty(PurchaseOrderDto? po)
    {
        if (po?.Items == null || po.Items.Count == 0) return "1 order";
        if (po.Items.Count == 1)
        {
            var item = po.Items.First();
            return $"{item.Quantity:0.##} {GetProductUnit(item.ProductID)}";
        }
        var totalQty = po.Items.Sum(i => i.Quantity);
        var unit = GetProductUnit(po.Items.First().ProductID);
        return $"{totalQty:0.##} {unit} ({po.Items.Count} items)";
    }

    private int PurchaseRequestsCount { get; set; } = 0;
    private int PendingQuotationsCount { get; set; } = 0;
    private int ApprovedRequestsCount { get; set; } = 0;
    private int RejectedRequestsCount { get; set; } = 0;
    private int ActiveContractsCount { get; set; } = 0;
    private int ActiveOrdersCount => OrdersForMyOutlet.Count(p => p.Status == "Dispatched" || p.Status == "Accepted");

    // PO Dashboard 3-Card Metrics
    private int PendingOrdersCount => OrdersForMyOutlet.Count(p => string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase));
    private int DispatchedOrdersCount => OrdersForMyOutlet.Count(p => string.Equals(p.Status, "Dispatched", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "In Transit", StringComparison.OrdinalIgnoreCase));
    private int DeliveredOrdersCount => OrdersForMyOutlet.Count(p => string.Equals(p.Status, "Delivered", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "Completed", StringComparison.OrdinalIgnoreCase));

    private string PoSearchQuery { get; set; } = string.Empty;

    private List<PurchaseOrderDto> FilteredPOs
    {
        get
        {
            var list = OrdersForMyOutlet.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(PoSearchQuery))
            {
                var q = PoSearchQuery.Trim().ToLowerInvariant();
                list = list.Where(p =>
                    $"PO-{p.PurchaseOrderID}".ToLowerInvariant().Contains(q) ||
                    $"PO-#{p.PurchaseOrderID}".ToLowerInvariant().Contains(q) ||
                    p.PurchaseOrderID.ToString().Contains(q) ||
                    (!string.IsNullOrEmpty(p.VendorName) && p.VendorName.ToLowerInvariant().Contains(q)) ||
                    (p.Items != null && p.Items.Any(i => GetProductName(i.ProductID).ToLowerInvariant().Contains(q)))
                );
            }
            return list.OrderByDescending(p => p.PurchaseOrderID).ToList();
        }
    }

    private bool ShowNotificationDropdown { get; set; } = false;
    private List<NotificationDto> Notifications { get; set; } = new();
    private int UnreadNotificationCount => Notifications.Count(n => !n.IsRead);

    private List<OutletActivityItem> RecentActivities { get; set; } = new();

    // Search & Filters for My Purchase Requests
    private string SearchQuery { get; set; } = string.Empty;
    private string StatusFilter { get; set; } = "All";
    private DateTime? FromDateFilter { get; set; }
    private DateTime? ToDateFilter { get; set; }

    private List<PurchaseRequestDto> FilteredPRs
    {
        get
        {
            var list = PRsForMyOutlet.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim().ToLowerInvariant();
                list = list.Where(p =>
                    $"PR-{p.RequestID}".ToLowerInvariant().Contains(q) ||
                    p.RequestID.ToString().Contains(q) ||
                    (p.Items != null && p.Items.Any(i => !string.IsNullOrEmpty(i.ProductName) && i.ProductName.ToLowerInvariant().Contains(q)))
                );
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                if (StatusFilter == "Approved")
                {
                    list = list.Where(p => string.Equals(p.Status, "Approved", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "Accepted", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "Completed", StringComparison.OrdinalIgnoreCase));
                }
                else if (StatusFilter == "Pending")
                {
                    list = list.Where(p => string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "Submitted", StringComparison.OrdinalIgnoreCase));
                }
                else if (StatusFilter == "Rejected")
                {
                    list = list.Where(p => string.Equals(p.Status, "Rejected", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "Declined", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    list = list.Where(p => string.Equals(p.Status, StatusFilter, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (FromDateFilter.HasValue)
            {
                list = list.Where(p => p.RequestDate.Date >= FromDateFilter.Value.Date);
            }

            if (ToDateFilter.HasValue)
            {
                list = list.Where(p => p.RequestDate.Date <= ToDateFilter.Value.Date);
            }

            return list.ToList();
        }
    }

    private void ClearFilters()
    {
        SearchQuery = string.Empty;
        StatusFilter = "All";
        FromDateFilter = null;
        ToDateFilter = null;
    }

    // Donut Chart Metrics
    private int TotalRequests => PurchaseRequestsCount;
    private int ApprovedPercentage => TotalRequests > 0 ? (int)Math.Round((double)ApprovedRequestsCount / TotalRequests * 100) : 0;
    private int PendingPercentage => TotalRequests > 0 ? (int)Math.Round((double)PendingQuotationsCount / TotalRequests * 100) : 0;
    private int RejectedPercentage => TotalRequests > 0 ? Math.Max(0, 100 - ApprovedPercentage - PendingPercentage) : 0;

    protected override async Task OnInitializedAsync()
    {
        if (Auth.IsAuthenticated && (Auth.IsOutletManager || Auth.IsAdmin))
        {
            UserOutletId = Auth.OutletID ?? 0;
            UserOrgId = Auth.OrganizationID ?? 0;
            OutletManagerName = !string.IsNullOrWhiteSpace(Auth.UserName) && Auth.UserName != "Guest"
                ? Auth.UserName
                : (Auth.CurrentUser?.Name ?? "Outlet Manager");

            await LoadDashboardData();
        }
        else
        {
            IsLoading = false;
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!string.IsNullOrWhiteSpace(QueryTab) && QueryTab != ActiveTab)
        {
            ActiveTab = QueryTab;
        }

        if (QueryPoId.HasValue && QueryPoId.Value > 0 && SelectedPurchaseOrder?.PurchaseOrderID != QueryPoId.Value && !IsLoading)
        {
            await OpenPurchaseOrderDetails(QueryPoId.Value);
        }
    }

    private string GetGreeting()
    {
        var hour = DateTime.Now.Hour;
        if (hour < 12) return "Good morning";
        if (hour < 17) return "Good afternoon";
        return "Good evening";
    }

    private async Task LoadDashboardData()
    {
        IsLoading = true;
        StateHasChanged();

        try
        {
            var outletsTask = Api.GetOutletsAsync();
            var prsTask = Api.GetPurchaseRequestsAsync();
            var contractsTask = Api.GetContractsByOutletAsync(UserOutletId);
            var productsTask = Api.GetProductsAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var orgsTask = Api.GetOrganizationsAsync();
            var posTask = Api.GetPurchaseOrdersAsync();

            await Task.WhenAll(outletsTask, prsTask, contractsTask, productsTask, vendorsTask, orgsTask, posTask);

            var outlets = await outletsTask ?? new List<OutletDto>();
            var matchedOutlet = outlets.FirstOrDefault(o => o.OutletID == UserOutletId);
            if (matchedOutlet != null)
            {
                OutletNameDisplay = !string.IsNullOrWhiteSpace(matchedOutlet.OutletName)
                    ? matchedOutlet.OutletName
                    : (!string.IsNullOrWhiteSpace(matchedOutlet.Address) ? $"{matchedOutlet.Address} Outlet" : $"Outlet {matchedOutlet.OutletID}");
                AssignedOutletAddress = matchedOutlet.Address ?? string.Empty;
                if (matchedOutlet.OrganizationID > 0)
                {
                    UserOrgId = matchedOutlet.OrganizationID;
                }
                if (!string.IsNullOrWhiteSpace(matchedOutlet.OrganizationName))
                {
                    OrganizationNameDisplay = matchedOutlet.OrganizationName;
                }
            }
            else
            {
                OutletNameDisplay = UserOutletId > 0 ? $"Outlet #{UserOutletId}" : "Assigned Outlet";
            }

            var orgs = await orgsTask ?? new List<OrganizationDto>();
            var matchedOrg = orgs.FirstOrDefault(o => o.OrganizationID == UserOrgId);
            if (matchedOrg != null && !string.IsNullOrWhiteSpace(matchedOrg.OrganizationName))
            {
                OrganizationNameDisplay = matchedOrg.OrganizationName;
            }
            else if (UserOrgId > 0)
            {
                try
                {
                    var directOrg = await Api.GetOrganizationByIdAsync(UserOrgId);
                    if (directOrg != null && !string.IsNullOrWhiteSpace(directOrg.OrganizationName))
                    {
                        OrganizationNameDisplay = directOrg.OrganizationName;
                    }
                }
                catch { }
            }

            var allPRs = await prsTask ?? new List<PurchaseRequestDto>();
            PRsForMyOutlet = UserOutletId > 0 
                ? allPRs.Where(p => p.OutletID == UserOutletId).OrderByDescending(p => p.RequestID).ToList()
                : new List<PurchaseRequestDto>();

            PurchaseRequestsCount = PRsForMyOutlet.Count;
            PendingQuotationsCount = PRsForMyOutlet.Count(p => string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "Submitted", StringComparison.OrdinalIgnoreCase));
            ApprovedRequestsCount = PRsForMyOutlet.Count(p => string.Equals(p.Status, "Approved", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "Accepted", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "Completed", StringComparison.OrdinalIgnoreCase));
            RejectedRequestsCount = PRsForMyOutlet.Count(p => string.Equals(p.Status, "Rejected", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "Declined", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));

            var contracts = await contractsTask ?? new List<ContractDto>();
            ContractsForMyOutlet = UserOutletId > 0 
                ? contracts.Where(c => c.OutletID == UserOutletId).ToList()
                : new List<ContractDto>();

            var allPos = await posTask ?? new List<PurchaseOrderDto>();
            OrdersForMyOutlet = UserOutletId > 0 
                ? allPos.Where(p => p.OutletID == UserOutletId).OrderByDescending(p => p.PurchaseOrderID).ToList()
                : new List<PurchaseOrderDto>();

            var products = await productsTask ?? new List<ProductDto>();
            var vendors = await vendorsTask ?? new List<VendorDto>();
            var productDict = products.ToDictionary(p => p.ProductID, p => p);
            var vendorDict = vendors.ToDictionary(v => v.VendorID, v => v.VendorName);
            ProductLookup = productDict;

            foreach (var po in OrdersForMyOutlet)
            {
                if (string.IsNullOrWhiteSpace(po.VendorName) && vendorDict.TryGetValue(po.VendorID, out var vName))
                {
                    po.VendorName = vName;
                }
            }

            foreach (var c in ContractsForMyOutlet)
            {
                if (string.IsNullOrWhiteSpace(c.ProductName) && productDict.TryGetValue(c.ProductID, out var pObj))
                {
                    c.ProductName = pObj.ProductName;
                    if (string.IsNullOrWhiteSpace(c.Unit))
                        c.Unit = pObj.Unit;
                }
                if (string.IsNullOrWhiteSpace(c.VendorName))
                {
                    int vId = c.VendorID ?? c.Allocations?.FirstOrDefault()?.VendorID ?? 0;
                    if (vId > 0 && vendorDict.TryGetValue(vId, out var vName))
                    {
                        c.VendorName = vName;
                    }
                    else if (c.Allocations != null && c.Allocations.Count > 0)
                    {
                        var primaryAlloc = c.Allocations.OrderByDescending(a => a.AllocationPercentage).FirstOrDefault();
                        if (primaryAlloc != null && !string.IsNullOrWhiteSpace(primaryAlloc.VendorName))
                            c.VendorName = primaryAlloc.VendorName;
                    }
                }
            }

            ActiveContractsCount = ContractsForMyOutlet.Count;

            await LoadNotifications();
            BuildRecentActivities();

            if (!string.IsNullOrWhiteSpace(QueryTab))
            {
                ActiveTab = QueryTab;
            }
            if (QueryPoId.HasValue && QueryPoId.Value > 0)
            {
                await OpenPurchaseOrderDetails(QueryPoId.Value);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OutletManagerDashboard] Error loading data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task LoadNotifications()
    {
        try
        {
            Notifications = await Api.GetMyNotificationsAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OutletManagerDashboard] Error loading notifications: {ex.Message}");
        }
    }

    private void BuildRecentActivities()
    {
        var list = new List<OutletActivityItem>();

        // 1. Incorporate notifications first
        if (Notifications != null)
        {
            foreach (var n in Notifications.Take(5))
            {
                string actType = "pending";
                if (n.Title.Contains("Approved", StringComparison.OrdinalIgnoreCase) || n.Message.Contains("Approved", StringComparison.OrdinalIgnoreCase))
                    actType = "approved";
                else if (n.Title.Contains("Rejected", StringComparison.OrdinalIgnoreCase) || n.Title.Contains("Declined", StringComparison.OrdinalIgnoreCase) || n.Message.Contains("Declined", StringComparison.OrdinalIgnoreCase))
                    actType = "rejected";
                else if (n.Title.Contains("Contract", StringComparison.OrdinalIgnoreCase))
                    actType = "contract";

                list.Add(new OutletActivityItem
                {
                    Text = n.Message,
                    Type = actType,
                    TimeAgo = GetTimeAgo(n.CreatedDate),
                    Timestamp = n.CreatedDate
                });
            }
        }

        // 2. Add recent PR events if notifications are scarce
        if (list.Count < 4 && PRsForMyOutlet != null)
        {
            foreach (var pr in PRsForMyOutlet.Take(6))
            {
                string refCode = $"PR-{pr.RequestID}";
                string status = pr.Status?.ToLowerInvariant() ?? "pending";

                if (status == "approved" || status == "accepted" || status == "completed")
                {
                    list.Add(new OutletActivityItem
                    {
                        Prefix = "Purchase Request ",
                        HighlightRef = refCode,
                        Suffix = " has been approved",
                        Type = "approved",
                        TimeAgo = GetTimeAgo(pr.RequestDate),
                        Timestamp = pr.RequestDate
                    });
                }
                else if (status == "rejected" || status == "declined")
                {
                    list.Add(new OutletActivityItem
                    {
                        Prefix = "Purchase Request ",
                        HighlightRef = refCode,
                        Suffix = " has been rejected",
                        Type = "rejected",
                        TimeAgo = GetTimeAgo(pr.RequestDate),
                        Timestamp = pr.RequestDate
                    });
                }
                else
                {
                    list.Add(new OutletActivityItem
                    {
                        Prefix = "Quotation received for ",
                        HighlightRef = refCode,
                        Suffix = "",
                        Type = "pending",
                        TimeAgo = GetTimeAgo(pr.RequestDate),
                        Timestamp = pr.RequestDate
                    });
                }
            }
        }

        // 3. Add contract events
        if (ContractsForMyOutlet != null)
        {
            foreach (var c in ContractsForMyOutlet.Take(2))
            {
                list.Add(new OutletActivityItem
                {
                    Prefix = "Contract ",
                    HighlightRef = $"CT-{c.ContractID}",
                    Suffix = " has been activated",
                    Type = "contract",
                    TimeAgo = GetTimeAgo(c.StartDate),
                    Timestamp = c.StartDate
                });
            }
        }

        RecentActivities = list.OrderByDescending(a => a.Timestamp).Take(5).ToList();
    }

    private string GetTimeAgo(DateTime dt)
    {
        var span = DateTime.UtcNow - dt.ToUniversalTime();
        if (span.TotalHours < 1) return "Just now";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} hours ago";
        if (span.TotalDays < 2) return "Yesterday";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays} days ago";
        return dt.ToString("dd MMM yyyy");
    }

    private void ToggleNotifications()
    {
        ShowNotificationDropdown = !ShowNotificationDropdown;
    }

    private async Task MarkAsRead(int notificationId)
    {
        await Api.MarkNotificationReadAsync(notificationId);
        var notif = Notifications.FirstOrDefault(n => n.NotificationID == notificationId);
        if (notif != null)
        {
            notif.IsRead = true;
        }
        StateHasChanged();
    }

    private void SetActiveTab(string tabName)
    {
        ActiveTab = tabName;
    }

    private void NavigateToRequest(int requestId)
    {
        Nav.NavigateTo($"/purchase-request/{requestId}");
    }

    private bool IsClickableNotification(NotificationDto notif)
    {
        if (notif == null) return false;

        if (notif.NotificationType.StartsWith("PurchaseOrder", StringComparison.OrdinalIgnoreCase) ||
            notif.Title.Contains("Purchase Order", StringComparison.OrdinalIgnoreCase))
        {
            return notif.RelatedRequestID.HasValue && notif.RelatedRequestID.Value > 0;
        }

        if (notif.NotificationType.Contains("Request", StringComparison.OrdinalIgnoreCase) ||
            notif.NotificationType.Contains("Opportunity", StringComparison.OrdinalIgnoreCase) ||
            notif.Title.Contains("Purchase Request", StringComparison.OrdinalIgnoreCase))
        {
            return notif.RelatedRequestID.HasValue && notif.RelatedRequestID.Value > 0;
        }

        return false;
    }

    private async Task HandleNotificationClick(NotificationDto notif)
    {
        if (notif == null) return;

        if (!notif.IsRead)
        {
            await MarkAsRead(notif.NotificationID);
        }

        ShowNotificationDropdown = false;
        PoErrorMessage = string.Empty;

        // Authoritative PO navigation via RelatedRequestID
        if (notif.NotificationType.StartsWith("PurchaseOrder", StringComparison.OrdinalIgnoreCase) ||
            notif.Title.Contains("Purchase Order", StringComparison.OrdinalIgnoreCase))
        {
            if (notif.RelatedRequestID.HasValue && notif.RelatedRequestID.Value > 0)
            {
                await OpenPurchaseOrderDetails(notif.RelatedRequestID.Value);
                return;
            }
            else
            {
                PoErrorMessage = "No valid Purchase Order ID was found on this notification.";
                StateHasChanged();
                return;
            }
        }

        // Authoritative PR navigation via RelatedRequestID
        if (notif.NotificationType.Contains("Request", StringComparison.OrdinalIgnoreCase) ||
            notif.NotificationType.Contains("Opportunity", StringComparison.OrdinalIgnoreCase) ||
            notif.Title.Contains("Purchase Request", StringComparison.OrdinalIgnoreCase))
        {
            if (notif.RelatedRequestID.HasValue && notif.RelatedRequestID.Value > 0)
            {
                NavigateToRequest(notif.RelatedRequestID.Value);
                return;
            }
        }
    }

    private List<DeliveryRecordDto> DeliveryRecordsForSelectedPo { get; set; } = new();
    private bool IsRecordingDelivery { get; set; } = false;
    private bool IsReviewingDelivery { get; set; } = false;
    private bool IsSubmittingDelivery { get; set; } = false;
    private decimal DeliveryReceivedQuantity { get; set; } = 0;
    private decimal DeliverySpoiledQuantity { get; set; } = 0;
    private DateTime DeliveryActualDate { get; set; } = DateTime.Now;
    private string DeliveryNotes { get; set; } = string.Empty;
    private string DeliveryFormError { get; set; } = string.Empty;
    private string DeliverySuccessMessage { get; set; } = string.Empty;

    private async Task OpenPurchaseOrderDetails(int poId)
    {
        PoErrorMessage = string.Empty;
        DeliveryFormError = string.Empty;
        DeliverySuccessMessage = string.Empty;
        IsRecordingDelivery = false;
        IsReviewingDelivery = false;

        try
        {
            var po = await Api.GetPurchaseOrderByIdAsync(poId);
            if (po != null)
            {
                // Enrich vendor name if missing
                if (string.IsNullOrWhiteSpace(po.VendorName))
                {
                    var vendors = await Api.GetVendorsAsync() ?? new List<VendorDto>();
                    var vMatch = vendors.FirstOrDefault(v => v.VendorID == po.VendorID);
                    if (vMatch != null)
                    {
                        po.VendorName = vMatch.VendorName;
                    }
                }

                SelectedPurchaseOrder = po;

                // Load delivery records if any
                try
                {
                    DeliveryRecordsForSelectedPo = await Api.GetDeliveryRecordsByPurchaseOrderAsync(poId) ?? new List<DeliveryRecordDto>();
                }
                catch
                {
                    DeliveryRecordsForSelectedPo = new List<DeliveryRecordDto>();
                }

                ShowPoDetailsModal = true;
                ActiveTab = "PurchaseOrders";
                StateHasChanged();
            }
            else
            {
                PoErrorMessage = $"Unable to load Purchase Order PO-#{poId}. You may not be authorized to view orders belonging to other outlets, or the order does not exist.";
                SelectedPurchaseOrder = null;
                ShowPoDetailsModal = false;
                ActiveTab = "PurchaseOrders";
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            PoErrorMessage = $"Error loading Purchase Order PO-#{poId}: {ex.Message}";
            SelectedPurchaseOrder = null;
            ShowPoDetailsModal = false;
            ActiveTab = "PurchaseOrders";
            StateHasChanged();
        }
    }

    private void StartRecordDelivery()
    {
        if (SelectedPurchaseOrder == null) return;

        var firstItem = SelectedPurchaseOrder.Items?.FirstOrDefault();
        decimal orderedQty = firstItem?.Quantity ?? 0;

        var alreadyReceived = DeliveryRecordsForSelectedPo?
            .Where(d => d.POItemID == firstItem?.POItemID && !string.Equals(d.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
            .Sum(d => d.ReceivedQuantity) ?? 0;

        decimal remainingQty = Math.Max(0, orderedQty - alreadyReceived);

        DeliveryReceivedQuantity = remainingQty > 0 ? remainingQty : orderedQty;
        DeliverySpoiledQuantity = 0;
        DeliveryActualDate = DateTime.Now;
        DeliveryNotes = string.Empty;
        DeliveryFormError = string.Empty;
        IsRecordingDelivery = true;
        IsReviewingDelivery = false;
    }

    private void CancelRecordDelivery()
    {
        IsRecordingDelivery = false;
        IsReviewingDelivery = false;
        DeliveryFormError = string.Empty;
    }

    private void ProceedToReviewDelivery()
    {
        DeliveryFormError = string.Empty;

        if (SelectedPurchaseOrder == null)
        {
            DeliveryFormError = "No active purchase order selected.";
            return;
        }

        var firstItem = SelectedPurchaseOrder.Items?.FirstOrDefault();
        decimal orderedQty = firstItem?.Quantity ?? 0;

        if (DeliveryReceivedQuantity <= 0)
        {
            DeliveryFormError = "Received quantity must be greater than zero.";
            return;
        }

        if (DeliverySpoiledQuantity < 0)
        {
            DeliveryFormError = "Spoiled/Damaged quantity cannot be negative.";
            return;
        }

        if (DeliverySpoiledQuantity > DeliveryReceivedQuantity)
        {
            DeliveryFormError = "Spoiled quantity cannot be greater than received quantity.";
            return;
        }

        var alreadyReceived = DeliveryRecordsForSelectedPo?
            .Where(d => d.POItemID == firstItem?.POItemID && string.Equals(d.Status, "Confirmed", StringComparison.OrdinalIgnoreCase))
            .Sum(d => d.ReceivedQuantity) ?? 0;

        decimal remainingQty = orderedQty - alreadyReceived;
        if (remainingQty <= 0)
        {
            DeliveryFormError = "The ordered quantity for this purchase order has already been fully received.";
            return;
        }

        if (DeliveryReceivedQuantity > remainingQty)
        {
            DeliveryFormError = $"Received quantity ({DeliveryReceivedQuantity:0.##}) cannot exceed the remaining quantity of {remainingQty:0.##}.";
            return;
        }

        if (DeliveryActualDate > DateTime.Now.AddDays(1))
        {
            DeliveryFormError = "Delivery date cannot be in the future.";
            return;
        }

        IsReviewingDelivery = true;
    }

    private void BackToEditDelivery()
    {
        IsReviewingDelivery = false;
    }

    private async Task ConfirmDeliveryAsync()
    {
        if (SelectedPurchaseOrder == null) return;

        DeliveryFormError = string.Empty;
        IsSubmittingDelivery = true;
        StateHasChanged();

        try
        {
            var firstItem = SelectedPurchaseOrder.Items?.FirstOrDefault();
            int poItemId = firstItem?.POItemID ?? 0;

            // 1. Create delivery record
            var createCmd = new CreateDeliveryRecordCommand
            {
                PurchaseOrderID = SelectedPurchaseOrder.PurchaseOrderID,
                POItemID = poItemId,
                ReceivedQuantity = DeliveryReceivedQuantity,
                SpoiledQuantity = DeliverySpoiledQuantity,
                DeliveryDate = DeliveryActualDate
            };

            var createdRecord = await Api.CreateDeliveryRecordAsync(createCmd);
            if (createdRecord == null || createdRecord.DeliveryRecordID <= 0)
            {
                DeliveryFormError = "Failed to create delivery record. Please check order status or try again.";
                IsSubmittingDelivery = false;
                StateHasChanged();
                return;
            }

            // 2. Confirm delivery record
            var confirmCmd = new ConfirmDeliveryRecordCommand
            {
                DeliveryRecordID = createdRecord.DeliveryRecordID,
                ConfirmedByUserID = Auth.CurrentUser?.UserID ?? Auth.UserID
            };

            var confirmedRecord = await Api.ConfirmDeliveryRecordAsync(confirmCmd);
            if (confirmedRecord == null)
            {
                DeliveryFormError = "Delivery record was created but confirmation failed. Please refresh.";
                IsSubmittingDelivery = false;
                StateHasChanged();
                return;
            }

            // 3. Refresh Purchase Order and local state from backend
            var refreshedPo = await Api.GetPurchaseOrderByIdAsync(SelectedPurchaseOrder.PurchaseOrderID);
            if (refreshedPo != null)
            {
                if (string.IsNullOrWhiteSpace(refreshedPo.VendorName) && !string.IsNullOrWhiteSpace(SelectedPurchaseOrder.VendorName))
                {
                    refreshedPo.VendorName = SelectedPurchaseOrder.VendorName;
                }
                SelectedPurchaseOrder = refreshedPo;

                // Update in OrdersForMyOutlet list as well
                var idx = OrdersForMyOutlet.FindIndex(p => p.PurchaseOrderID == refreshedPo.PurchaseOrderID);
                if (idx >= 0)
                {
                    OrdersForMyOutlet[idx] = refreshedPo;
                }
            }

            DeliveryRecordsForSelectedPo = await Api.GetDeliveryRecordsByPurchaseOrderAsync(SelectedPurchaseOrder.PurchaseOrderID) ?? new List<DeliveryRecordDto>();

            IsRecordingDelivery = false;
            IsReviewingDelivery = false;
            DeliverySuccessMessage = $"Delivery for PO-#{SelectedPurchaseOrder.PurchaseOrderID} confirmed successfully! Status updated to Delivered.";
            
            await LoadNotifications();
        }
        catch (Exception ex)
        {
            DeliveryFormError = $"An error occurred during delivery confirmation: {ex.Message}";
        }
        finally
        {
            IsSubmittingDelivery = false;
            StateHasChanged();
        }
    }

    private string GetDeliveryPerformanceText(PurchaseOrderDto? po, DeliveryRecordDto? dr)
    {
        if (po == null) return "Pending";
        DateTime actualDate = dr?.DeliveryDate ?? po.ActualDeliveryDate ?? DateTime.Now;
        DateTime expectedDate = po.ExpectedDeliveryDate ?? po.OrderDate.AddDays(2);

        if (actualDate.Date <= expectedDate.Date)
        {
            var diff = (expectedDate.Date - actualDate.Date).Days;
            if (diff > 0)
            {
                return $"On Time (Delivered {diff} day{(diff == 1 ? "" : "s")} before expected delivery)";
            }
            return "On Time";
        }
        else
        {
            var delayedDays = (actualDate.Date - expectedDate.Date).Days;
            return $"Delayed by {delayedDays} day{(delayedDays == 1 ? "" : "s")}";
        }
    }

    private string GetDeliveryPerformanceBadge(PurchaseOrderDto? po, DeliveryRecordDto? dr)
    {
        if (po == null) return "badge bg-secondary";
        DateTime actualDate = dr?.DeliveryDate ?? po.ActualDeliveryDate ?? DateTime.Now;
        DateTime expectedDate = po.ExpectedDeliveryDate ?? po.OrderDate.AddDays(2);

        if (actualDate.Date <= expectedDate.Date)
        {
            return "badge bg-success";
        }
        return "badge bg-danger";
    }

    private void ClosePoDetailsModal()
    {
        ShowPoDetailsModal = false;
        SelectedPurchaseOrder = null;
        IsRecordingDelivery = false;
        IsReviewingDelivery = false;
        PoErrorMessage = string.Empty;
        DeliveryFormError = string.Empty;
        DeliverySuccessMessage = string.Empty;
    }

    private bool IsEditingOutlet { get; set; } = false;
    private bool IsSavingOutlet { get; set; } = false;
    private string EditOutletName { get; set; } = string.Empty;
    private string EditOutletAddress { get; set; } = string.Empty;
    private string OutletMessage { get; set; } = string.Empty;
    private string OutletError { get; set; } = string.Empty;

    private void StartEditOutlet()
    {
        EditOutletName = OutletNameDisplay;
        EditOutletAddress = AssignedOutletAddress;
        OutletMessage = string.Empty;
        OutletError = string.Empty;
        IsEditingOutlet = true;
    }

    private void CancelEditOutlet()
    {
        IsEditingOutlet = false;
        OutletMessage = string.Empty;
        OutletError = string.Empty;
    }

    private async Task SaveOutletDetails()
    {
        OutletMessage = string.Empty;
        OutletError = string.Empty;

        if (string.IsNullOrWhiteSpace(EditOutletName))
        {
            OutletError = "Outlet name is required.";
            return;
        }

        IsSavingOutlet = true;
        try
        {
            var cmd = new UpdateOutletCommand
            {
                OutletID = UserOutletId,
                OrganizationID = UserOrgId,
                OutletName = EditOutletName.Trim(),
                Address = EditOutletAddress?.Trim()
            };

            var res = await Api.UpdateOutletAsync(UserOutletId, cmd);
            if (res != null && res.Success)
            {
                OutletNameDisplay = EditOutletName.Trim();
                AssignedOutletAddress = EditOutletAddress?.Trim() ?? string.Empty;
                OutletMessage = "Outlet details updated successfully.";
                IsEditingOutlet = false;
            }
            else
            {
                OutletError = res?.ErrorMessage ?? "Failed to update outlet details.";
            }
        }
        catch (Exception)
        {
            OutletError = "An error occurred while updating outlet details.";
        }
        finally
        {
            IsSavingOutlet = false;
            StateHasChanged();
        }
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login");
    }

    public class OutletActivityItem
    {
        public string Text { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string HighlightRef { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public string Type { get; set; } = "pending"; // approved, pending, rejected, contract
        public string TimeAgo { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}