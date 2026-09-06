using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.OrgPurchaseOrders;

public partial class OrgPurchaseOrders : ComponentBase
{
    [Parameter]
    public int? PurchaseOrderId { get; set; }

    [SupplyParameterFromQuery(Name = "id")]
    public int? QueryPoId { get; set; }

    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;
    private string? ErrorMessage { get; set; }

    private List<PurchaseOrderDto> AllPurchaseOrders { get; set; } = new();
    private List<OutletDto> OrgOutlets { get; set; } = new();
    private List<ProductDto> AllProducts { get; set; } = new();
    private List<VendorDto> AllVendors { get; set; } = new();
    private List<ContractDto> AllContracts { get; set; } = new();
    private List<QuotationDto> AcceptedQuotations { get; set; } = new();
    private List<PurchaseRequestDto> AllPurchaseRequests { get; set; } = new();

    private Dictionary<int, string> OutletNames { get; set; } = new();
    private Dictionary<int, OutletDto> OutletsMap { get; set; } = new();
    private Dictionary<int, string> ProductNames { get; set; } = new();
    private Dictionary<int, ProductDto> ProductsMap { get; set; } = new();
    private Dictionary<int, string> VendorNames { get; set; } = new();
    private Dictionary<int, VendorDto> VendorsMap { get; set; } = new();

    private string OrganizationName { get; set; } = "Organization";
    private string SearchQuery { get; set; } = string.Empty;
    private string StatusFilter { get; set; } = "All";
    private int SelectedOutletId { get; set; } = 0;

    // Sidebar & Profile state
    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;
    private bool ShowNotificationDropdown { get; set; } = false;
    private List<NotificationDto> Notifications { get; set; } = new();
    private int UnreadNotificationCount => Notifications.Count(n => !n.IsRead);

    // Selected PO Detail View State (Matching Screenshot 2)
    private PurchaseOrderDto? SelectedPoForDetail { get; set; }
    private List<DeliveryRecordDto> SelectedPoDeliveryRecords { get; set; } = new();
    private bool IsViewingPoDetails => SelectedPoForDetail != null;

    // Delivery Recording State (Phase 2Q-C for Purchase Manager)
    private bool IsRecordingDelivery { get; set; } = false;
    private bool IsReviewingDelivery { get; set; } = false;
    private bool IsSubmittingDelivery { get; set; } = false;
    private decimal DeliveryReceivedQuantity { get; set; } = 0;
    private decimal DeliverySpoiledQuantity { get; set; } = 0;
    private DateTime DeliveryActualDate { get; set; } = DateTime.Now;
    private string DeliveryNotes { get; set; } = string.Empty;
    private string DeliveryFormError { get; set; } = string.Empty;
    private string DeliverySuccessMessage { get; set; } = string.Empty;

    // Create PO Modal State
    private bool ShowCreatePoModal { get; set; } = false;
    private bool IsProcessingPo { get; set; } = false;
    private string? ModalErrorMessage { get; set; }
    private string? ActionSuccessMessage { get; set; }
    private string? ActionErrorMessage { get; set; }
    private bool IsActionProcessing { get; set; } = false;
    private int SelectedQuotationId { get; set; } = 0;
    private DateTime ExpectedDeliveryDate { get; set; } = DateTime.Today.AddDays(3);

    public class QuotationPoOption
    {
        public int QuotationID { get; set; }
        public int RequestID { get; set; }
        public int VendorID { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int OutletID { get; set; }
        public string OutletName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "Kg";
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public bool HasActiveContract { get; set; }
        public decimal RemainingContractQty { get; set; }
        public string RecommendationBadge { get; set; } = "Alternative Eligible Vendor";
        public bool IsRecommended { get; set; }
    }

    private List<QuotationPoOption> EligiblePoOptions { get; set; } = new();

    private List<PurchaseOrderDto> FilteredPurchaseOrders
    {
        get
        {
            var list = AllPurchaseOrders.AsEnumerable();

            if (!Auth.IsPurchaseManager && SelectedOutletId > 0)
            {
                list = list.Where(po => po.OutletID == SelectedOutletId);
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "All")
            {
                if (string.Equals(StatusFilter, "Awaiting Approval", StringComparison.OrdinalIgnoreCase) || string.Equals(StatusFilter, "AwaitingApproval", StringComparison.OrdinalIgnoreCase))
                {
                    list = list.Where(po => string.Equals(po.Status, "Awaiting Approval", StringComparison.OrdinalIgnoreCase) || string.Equals(po.Status, "AwaitingApproval", StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    list = list.Where(po => string.Equals(po.Status, StatusFilter, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim().ToLowerInvariant();
                list = list.Where(po =>
                    $"PO-{po.PurchaseOrderID}".ToLowerInvariant().Contains(q) ||
                    po.PurchaseOrderID.ToString().Contains(q) ||
                    (VendorNames.TryGetValue(po.VendorID, out var vName) && vName.ToLowerInvariant().Contains(q)) ||
                    (OutletNames.TryGetValue(po.OutletID, out var oName) && oName.ToLowerInvariant().Contains(q)) ||
                    (po.Items != null && po.Items.Any(i => ProductNames.TryGetValue(i.ProductID, out var pName) && pName.ToLowerInvariant().Contains(q)))
                );
            }

            return list.OrderByDescending(po => po.PurchaseOrderID).ToList();
        }
    }

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
        return Auth.IsPurchaseManager ? "P" : "G";
    }

    protected override async Task OnInitializedAsync()
    {
        if (Auth.IsAuthenticated && (Auth.IsOrgManager || Auth.IsAdmin || Auth.IsPurchaseManager))
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
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            var poTask = Api.GetPurchaseOrdersAsync();
            var outletsTask = Api.GetOutletsAsync();
            var productsTask = Api.GetProductsAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var contractsTask = Api.GetContractsAsync();
            var quotationsTask = Api.GetQuotationsAsync();
            var prsTask = Api.GetPurchaseRequestsAsync();
            var orgsTask = Api.GetOrganizationsAsync();

            await Task.WhenAll(poTask, outletsTask, productsTask, vendorsTask, contractsTask, quotationsTask, prsTask, orgsTask);

            var orders = await poTask ?? new List<PurchaseOrderDto>();
            var outlets = await outletsTask ?? new List<OutletDto>();
            AllProducts = await productsTask ?? new List<ProductDto>();
            AllVendors = await vendorsTask ?? new List<VendorDto>();
            AllContracts = await contractsTask ?? new List<ContractDto>();
            var quotations = await quotationsTask ?? new List<QuotationDto>();
            AllPurchaseRequests = await prsTask ?? new List<PurchaseRequestDto>();
            var orgs = await orgsTask ?? new List<OrganizationDto>();

            int userOrgId = Auth.OrganizationID ?? 0;
            var orgObj = orgs.FirstOrDefault(o => o.OrganizationID == userOrgId);
            if (orgObj != null && !string.IsNullOrWhiteSpace(orgObj.OrganizationName))
            {
                OrganizationName = orgObj.OrganizationName;
            }

            // Populate Dictionaries
            ProductsMap = AllProducts.ToDictionary(p => p.ProductID, p => p);
            VendorsMap = AllVendors.ToDictionary(v => v.VendorID, v => v);

            ProductNames = AllProducts.ToDictionary(
                p => p.ProductID,
                p => !string.IsNullOrWhiteSpace(p.ProductName) ? p.ProductName : $"Product #{p.ProductID}"
            );

            VendorNames = AllVendors.ToDictionary(
                v => v.VendorID,
                v => !string.IsNullOrWhiteSpace(v.VendorName) ? v.VendorName : $"Vendor #{v.VendorID}"
            );

            // Scoping based on Role
            if (Auth.IsPurchaseManager)
            {
                if (Auth.OutletID.HasValue && Auth.OutletID.Value > 0)
                {
                    int pmOutletId = Auth.OutletID.Value;
                    OrgOutlets = outlets.Where(o => o.OutletID == pmOutletId).ToList();
                    OutletsMap = OrgOutlets.ToDictionary(o => o.OutletID, o => o);
                    OutletNames = OrgOutlets.ToDictionary(
                        o => o.OutletID,
                        o => !string.IsNullOrWhiteSpace(o.OutletName) ? o.OutletName : (!string.IsNullOrWhiteSpace(o.Address) ? $"{o.Address.Split(',')[0].Trim()} Outlet" : $"Outlet #{o.OutletID}")
                    );
                    if (!OutletNames.ContainsKey(pmOutletId))
                    {
                        var matchedOutl = outlets.FirstOrDefault(o => o.OutletID == pmOutletId);
                        OutletNames[pmOutletId] = matchedOutl != null && !string.IsNullOrWhiteSpace(matchedOutl.OutletName) ? matchedOutl.OutletName : $"Outlet #{pmOutletId}";
                    }

                    AllPurchaseOrders = orders.Where(po => po.OutletID == pmOutletId).ToList();

                    var existingPoQuotationIds = AllPurchaseOrders.Select(po => po.QuotationID).ToHashSet();
                    AcceptedQuotations = quotations
                        .Where(q => string.Equals(q.Status, "Accepted", StringComparison.OrdinalIgnoreCase) && 
                                    !existingPoQuotationIds.Contains(q.QuotationID) &&
                                    (AllPurchaseRequests.FirstOrDefault(pr => pr.RequestID == q.RequestID) is var matchPr && matchPr != null && matchPr.OutletID == pmOutletId))
                        .OrderByDescending(q => q.QuotationID)
                        .ToList();
                }
                else
                {
                    OrgOutlets = new List<OutletDto>();
                    OutletsMap = new Dictionary<int, OutletDto>();
                    OutletNames = new Dictionary<int, string>();
                    AllPurchaseOrders = new List<PurchaseOrderDto>();
                    AcceptedQuotations = new List<QuotationDto>();
                    HasError = true;
                    ErrorMessage = "No outlet is assigned to this Purchase Manager.";
                }
            }
            else if (userOrgId > 0)
            {
                OrgOutlets = outlets.Where(o => o.OrganizationID == userOrgId).ToList();
                OutletsMap = OrgOutlets.ToDictionary(o => o.OutletID, o => o);
                OutletNames = OrgOutlets.ToDictionary(
                    o => o.OutletID,
                    o => !string.IsNullOrWhiteSpace(o.OutletName) ? o.OutletName : (!string.IsNullOrWhiteSpace(o.Address) ? $"{o.Address.Split(',')[0].Trim()} Outlet" : $"Outlet #{o.OutletID}")
                );

                var orgOutletIds = OrgOutlets.Select(o => o.OutletID).ToHashSet();
                AllPurchaseOrders = orgOutletIds.Count > 0
                    ? orders.Where(po => orgOutletIds.Contains(po.OutletID)).ToList()
                    : new List<PurchaseOrderDto>();

                var existingPoQuotationIds = AllPurchaseOrders.Select(po => po.QuotationID).ToHashSet();
                AcceptedQuotations = quotations
                    .Where(q => string.Equals(q.Status, "Accepted", StringComparison.OrdinalIgnoreCase) && 
                                !existingPoQuotationIds.Contains(q.QuotationID) &&
                                (AllPurchaseRequests.FirstOrDefault(pr => pr.RequestID == q.RequestID) is var matchPr && matchPr != null && orgOutletIds.Contains(matchPr.OutletID)))
                    .OrderByDescending(q => q.QuotationID)
                    .ToList();
            }
            else
            {
                OrgOutlets = outlets;
                OutletsMap = OrgOutlets.ToDictionary(o => o.OutletID, o => o);
                OutletNames = OrgOutlets.ToDictionary(
                    o => o.OutletID,
                    o => !string.IsNullOrWhiteSpace(o.OutletName) ? o.OutletName : (!string.IsNullOrWhiteSpace(o.Address) ? $"{o.Address.Split(',')[0].Trim()} Outlet" : $"Outlet #{o.OutletID}")
                );

                AllPurchaseOrders = orders;
                var existingPoQuotationIds = AllPurchaseOrders.Select(po => po.QuotationID).ToHashSet();
                AcceptedQuotations = quotations
                    .Where(q => string.Equals(q.Status, "Accepted", StringComparison.OrdinalIgnoreCase) && 
                                !existingPoQuotationIds.Contains(q.QuotationID))
                    .OrderByDescending(q => q.QuotationID)
                    .ToList();
            }

            // Load notifications
            try
            {
                Notifications = await Api.GetMyNotificationsAsync();
            }
            catch
            {
                Notifications = new List<NotificationDto>();
            }

            // Build Options for creating PO with Contract recommendation ranking
            BuildEligiblePoOptions();

            // If ID parameter was passed in route or query, open it immediately
            int targetPoId = PurchaseOrderId ?? QueryPoId ?? 0;
            if (targetPoId > 0)
            {
                await OpenPoDetails(targetPoId, updateUrl: false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrgPurchaseOrders] LoadData error: {ex.Message}");
            HasError = true;
            ErrorMessage = "An error occurred while loading purchase orders data.";
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        int targetPoId = PurchaseOrderId ?? QueryPoId ?? 0;
        if (targetPoId > 0 && SelectedPoForDetail?.PurchaseOrderID != targetPoId && !IsLoading)
        {
            await OpenPoDetails(targetPoId, updateUrl: false);
        }
    }

    private async Task OpenPoDetails(int poId, bool updateUrl = true)
    {
        IsRecordingDelivery = false;
        IsReviewingDelivery = false;
        DeliveryFormError = string.Empty;
        DeliverySuccessMessage = string.Empty;

        try
        {
            var po = await Api.GetPurchaseOrderByIdAsync(poId);
            if (po != null)
            {
                if (string.IsNullOrWhiteSpace(po.VendorName) && VendorNames.TryGetValue(po.VendorID, out var vn))
                {
                    po.VendorName = vn;
                }

                SelectedPoForDetail = po;

                try
                {
                    SelectedPoDeliveryRecords = await Api.GetDeliveryRecordsByPurchaseOrderAsync(poId) ?? new List<DeliveryRecordDto>();
                }
                catch
                {
                    SelectedPoDeliveryRecords = new List<DeliveryRecordDto>();
                }

                if (updateUrl)
                {
                    Nav.NavigateTo($"/organization/purchase-orders?id={poId}", false);
                }
            }
            else
            {
                SelectedPoForDetail = null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrgPurchaseOrders] OpenPoDetails error: {ex.Message}");
            SelectedPoForDetail = null;
        }
        finally
        {
            StateHasChanged();
        }
    }

    private void BackToPoList()
    {
        SelectedPoForDetail = null;
        SelectedPoDeliveryRecords.Clear();
        IsRecordingDelivery = false;
        IsReviewingDelivery = false;
        DeliveryFormError = string.Empty;
        DeliverySuccessMessage = string.Empty;
        Nav.NavigateTo("/organization/purchase-orders", false);
        StateHasChanged();
    }

    private void ToggleNotifications()
    {
        ShowNotificationDropdown = !ShowNotificationDropdown;
    }

    private async Task MarkAsRead(int notificationId)
    {
        await Api.MarkNotificationReadAsync(notificationId);
        var n = Notifications.FirstOrDefault(x => x.NotificationID == notificationId);
        if (n != null) n.IsRead = true;
        StateHasChanged();
    }

    private async Task HandleNotificationClick(NotificationDto notif)
    {
        if (notif == null) return;
        if (!notif.IsRead)
        {
            await MarkAsRead(notif.NotificationID);
        }
        ShowNotificationDropdown = false;

        if (notif.RelatedRequestID.HasValue && notif.RelatedRequestID.Value > 0)
        {
            await OpenPoDetails(notif.RelatedRequestID.Value);
        }
    }

    private string GetProductUnit(int productId)
    {
        return ProductsMap.TryGetValue(productId, out var p) && !string.IsNullOrWhiteSpace(p.Unit) ? p.Unit : "units";
    }

    private string GetDeliveryPerformanceText(PurchaseOrderDto? po, DeliveryRecordDto? dr)
    {
        if (po == null) return "Pending";
        DateTime actualDate = dr?.DeliveryDate ?? po.ActualDeliveryDate ?? po.OrderDate;
        DateTime expectedDate = po.ExpectedDeliveryDate ?? po.OrderDate.AddDays(2);

        if (actualDate.Date <= expectedDate.Date)
        {
            int diff = (expectedDate.Date - actualDate.Date).Days;
            if (diff > 0)
            {
                return $"On Time (Delivered {diff} day{(diff == 1 ? "" : "s")} before expected delivery)";
            }
            return "On Time";
        }
        else
        {
            int delayedDays = (actualDate.Date - expectedDate.Date).Days;
            return $"Delayed by {delayedDays} day{(delayedDays == 1 ? "" : "s")}";
        }
    }

    private string GetDeliveryPerformanceBadge(PurchaseOrderDto? po, DeliveryRecordDto? dr)
    {
        if (po == null) return "badge bg-secondary";
        DateTime actualDate = dr?.DeliveryDate ?? po.ActualDeliveryDate ?? po.OrderDate;
        DateTime expectedDate = po.ExpectedDeliveryDate ?? po.OrderDate.AddDays(2);

        if (actualDate.Date <= expectedDate.Date)
        {
            return "badge bg-success";
        }
        return "badge bg-danger";
    }

    private void BuildEligiblePoOptions()
    {
        var options = new List<QuotationPoOption>();

        foreach (var q in AcceptedQuotations)
        {
            var pr = AllPurchaseRequests.FirstOrDefault(p => p.RequestID == q.RequestID);
            int outletId = pr?.OutletID ?? 0;
            var qItem = q.Items?.FirstOrDefault();
            int productId = qItem?.ProductID ?? 0;
            decimal qty = qItem?.Quantity ?? 0m;
            string unit = "Kg";

            if (pr?.Items != null && pr.Items.Count > 0)
            {
                var prItem = pr.Items.First();
                if (productId == 0) productId = prItem.ProductID;
                if (qty == 0) qty = prItem.Quantity;
                if (!string.IsNullOrEmpty(prItem.Unit)) unit = prItem.Unit;
            }

            // Check if vendor has an active contract for this product and outlet
            var matchingContract = AllContracts.FirstOrDefault(c =>
                c.OutletID == outletId &&
                c.ProductID == productId &&
                string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase) &&
                (c.VendorID == q.VendorID || (c.Allocations != null && c.Allocations.Any(a => a.VendorID == q.VendorID && string.Equals(a.Status, "Active", StringComparison.OrdinalIgnoreCase))))
            );

            bool hasContract = matchingContract != null;
            decimal remainingQty = 0m;

            if (matchingContract != null)
            {
                if (matchingContract.Allocations != null && matchingContract.Allocations.Count > 0)
                {
                    var alloc = matchingContract.Allocations.FirstOrDefault(a => a.VendorID == q.VendorID);
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

            string vName = VendorNames.TryGetValue(q.VendorID, out var vn) ? vn : $"Vendor #{q.VendorID}";
            string pName = ProductNames.TryGetValue(productId, out var pn) ? pn : $"Product #{productId}";
            string oName = OutletNames.TryGetValue(outletId, out var on) ? on : $"Outlet #{outletId}";

            bool isRecommended = hasContract && remainingQty > 0;
            string badge = isRecommended
                ? $"Recommended (Active Contract • Remaining: {remainingQty:N0} {unit})"
                : (hasContract ? "Active Contract (0 Remaining Capacity)" : "Alternative Eligible Vendor (No Active Contract)");

            options.Add(new QuotationPoOption
            {
                QuotationID = q.QuotationID,
                RequestID = q.RequestID,
                VendorID = q.VendorID,
                VendorName = vName,
                ProductID = productId,
                ProductName = pName,
                OutletID = outletId,
                OutletName = oName,
                Quantity = qty,
                Unit = unit,
                UnitPrice = qItem?.UnitPrice ?? 0m,
                TotalAmount = q.Items?.Sum(i => i.TotalAmount > 0 ? i.TotalAmount : (i.Quantity * i.UnitPrice + i.TaxAmount)) ?? 0m,
                HasActiveContract = hasContract,
                RemainingContractQty = remainingQty,
                RecommendationBadge = badge,
                IsRecommended = isRecommended
            });
        }

        // Rank Recommended Contract vendors first, then alternatives
        EligiblePoOptions = options
            .OrderByDescending(o => o.IsRecommended)
            .ThenByDescending(o => o.QuotationID)
            .ToList();
    }

    private void OpenCreateModal()
    {
        ModalErrorMessage = null;
        if (EligiblePoOptions.Count > 0)
        {
            SelectedQuotationId = EligiblePoOptions.First().QuotationID;
        }
        else
        {
            SelectedQuotationId = 0;
        }
        ExpectedDeliveryDate = DateTime.Today.AddDays(3);
        ShowCreatePoModal = true;
    }

    private void CloseCreateModal()
    {
        ShowCreatePoModal = false;
        ModalErrorMessage = null;
    }

    private async Task HandleSubmitPo()
    {
        if (SelectedQuotationId <= 0)
        {
            ModalErrorMessage = "Please select an accepted quotation to issue a Purchase Order.";
            return;
        }

        IsProcessingPo = true;
        ModalErrorMessage = null;
        ActionSuccessMessage = null;
        ActionErrorMessage = null;
        StateHasChanged();

        try
        {
            var command = new CreatePurchaseOrderCommand
            {
                QuotationID = SelectedQuotationId,
                ExpectedDeliveryDate = ExpectedDeliveryDate
            };

            var createdPo = await Api.CreatePurchaseOrderAsync(command);
            if (createdPo != null && createdPo.PurchaseOrderID > 0)
            {
                CloseCreateModal();
                ActionSuccessMessage = "Purchase Order created and sent for approval.";
                await LoadData();
            }
            else
            {
                ModalErrorMessage = "Unable to create Purchase Order. Please ensure quotation is accepted and not expired.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrgPurchaseOrders] Create PO error: {ex.Message}");
            ModalErrorMessage = "An error occurred while creating the Purchase Order.";
        }
        finally
        {
            IsProcessingPo = false;
            StateHasChanged();
        }
    }

    private async Task HandleApprovePo(int poId)
    {
        if (!Auth.IsOrgManager) return;

        IsActionProcessing = true;
        ActionSuccessMessage = null;
        ActionErrorMessage = null;
        StateHasChanged();

        try
        {
            var result = await Api.ApprovePurchaseOrderAsync(poId);
            if (result != null)
            {
                ActionSuccessMessage = "Purchase Order approved successfully.";
                if (SelectedPoForDetail != null && SelectedPoForDetail.PurchaseOrderID == poId)
                {
                    SelectedPoForDetail.Status = "Approved";
                }
                var item = AllPurchaseOrders.FirstOrDefault(p => p.PurchaseOrderID == poId);
                if (item != null) item.Status = "Approved";
                await LoadData();
                if (SelectedPoForDetail != null)
                {
                    await OpenPoDetails(poId, updateUrl: false);
                }
            }
            else
            {
                ActionErrorMessage = "Unable to approve Purchase Order. Please try again.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrgPurchaseOrders] Approve error: {ex.Message}");
            ActionErrorMessage = "An error occurred while approving the Purchase Order.";
        }
        finally
        {
            IsActionProcessing = false;
            StateHasChanged();
        }
    }

    private async Task HandleRejectPo(int poId)
    {
        if (!Auth.IsOrgManager) return;

        IsActionProcessing = true;
        ActionSuccessMessage = null;
        ActionErrorMessage = null;
        StateHasChanged();

        try
        {
            var result = await Api.RejectPurchaseOrderAsync(poId);
            if (result != null)
            {
                ActionSuccessMessage = "Purchase Order rejected.";
                if (SelectedPoForDetail != null && SelectedPoForDetail.PurchaseOrderID == poId)
                {
                    SelectedPoForDetail.Status = "Rejected";
                }
                var item = AllPurchaseOrders.FirstOrDefault(p => p.PurchaseOrderID == poId);
                if (item != null) item.Status = "Rejected";
                await LoadData();
                if (SelectedPoForDetail != null)
                {
                    await OpenPoDetails(poId, updateUrl: false);
                }
            }
            else
            {
                ActionErrorMessage = "Unable to reject Purchase Order. Please try again.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrgPurchaseOrders] Reject error: {ex.Message}");
            ActionErrorMessage = "An error occurred while rejecting the Purchase Order.";
        }
        finally
        {
            IsActionProcessing = false;
            StateHasChanged();
        }
    }

    private async Task HandleSendPo(int poId)
    {
        if (!Auth.IsPurchaseManager) return;

        IsActionProcessing = true;
        ActionSuccessMessage = null;
        ActionErrorMessage = null;
        StateHasChanged();

        try
        {
            var result = await Api.SendPurchaseOrderAsync(poId);
            if (result != null)
            {
                ActionSuccessMessage = "Purchase Order sent to vendor successfully.";
                if (SelectedPoForDetail != null && SelectedPoForDetail.PurchaseOrderID == poId)
                {
                    SelectedPoForDetail.Status = "Pending";
                }
                var item = AllPurchaseOrders.FirstOrDefault(p => p.PurchaseOrderID == poId);
                if (item != null) item.Status = "Pending";
                await LoadData();
                if (SelectedPoForDetail != null)
                {
                    await OpenPoDetails(poId, updateUrl: false);
                }
            }
            else
            {
                ActionErrorMessage = "Unable to send Purchase Order to vendor. Please try again.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrgPurchaseOrders] Send error: {ex.Message}");
            ActionErrorMessage = "An error occurred while sending the Purchase Order to vendor.";
        }
        finally
        {
            IsActionProcessing = false;
            StateHasChanged();
        }
    }

    private string GetStatusBadgeClass(string? status)
    {
        return status switch
        {
            "Awaiting Approval" or "AwaitingApproval" => "po-status-pending",
            "Approved" => "po-status-accepted",
            "Pending" => "po-status-pending",
            "Accepted" => "po-status-accepted",
            "Dispatched" => "po-status-dispatched",
            "Delivered" or "Completed" => "po-status-delivered",
            "Rejected" => "po-status-rejected",
            _ => "po-status-neutral"
        };
    }

    // =========================================================================
    // VENDOR FEEDBACK / REVIEW MODAL STATE & HANDLERS
    // =========================================================================
    private bool IsReviewModalOpen { get; set; } = false;
    private VenodorManagementFrontend.Models.VendorPerformance.EligibleReviewOrderDto? ReviewOrderItem { get; set; }
    private HashSet<int> ReviewedPoItemIds { get; set; } = new();

    private bool IsSelectedPoReviewed
    {
        get
        {
            if (SelectedPoForDetail?.Items == null || SelectedPoForDetail.Items.Count == 0) return false;
            return SelectedPoForDetail.Items.All(i => ReviewedPoItemIds.Contains(i.POItemID));
        }
    }

    private async Task OpenReviewModalForSelectedPo()
    {
        if (SelectedPoForDetail == null) return;
        var po = SelectedPoForDetail;
        var firstItem = po.Items?.FirstOrDefault();
        if (firstItem == null) return;

        // Fetch latest eligible review orders to check if this PO item was already reviewed
        bool alreadyReviewed = false;
        try
        {
            var eligibleList = await Api.GetEligibleReviewOrdersAsync();
            var match = eligibleList.FirstOrDefault(e => e.PurchaseOrderID == po.PurchaseOrderID && e.POItemID == firstItem.POItemID);
            if (match != null)
            {
                alreadyReviewed = match.AlreadyReviewed;
            }
            else if (ReviewedPoItemIds.Contains(firstItem.POItemID))
            {
                alreadyReviewed = true;
            }
        }
        catch
        {
            alreadyReviewed = ReviewedPoItemIds.Contains(firstItem.POItemID);
        }

        string pName = ProductNames.TryGetValue(firstItem.ProductID, out var pn) ? pn : "Delivered Product";
        string vName = !string.IsNullOrWhiteSpace(po.VendorName) ? po.VendorName : (VendorNames.TryGetValue(po.VendorID, out var vn) ? vn : $"Vendor #{po.VendorID}");
        string oName = OutletNames.TryGetValue(po.OutletID, out var on) ? on : $"Outlet #{po.OutletID}";
        string uName = GetProductUnit(firstItem.ProductID);

        DateTime? actualDate = po.ActualDeliveryDate ?? SelectedPoDeliveryRecords.FirstOrDefault()?.DeliveryDate;

        ReviewOrderItem = new VenodorManagementFrontend.Models.VendorPerformance.EligibleReviewOrderDto
        {
            PurchaseOrderID = po.PurchaseOrderID,
            POItemID = firstItem.POItemID,
            VendorID = po.VendorID,
            VendorName = vName,
            OutletID = po.OutletID,
            OutletName = oName,
            ProductID = firstItem.ProductID,
            ProductName = pName,
            Quantity = firstItem.Quantity,
            Unit = uName,
            ExpectedDeliveryDate = po.ExpectedDeliveryDate,
            ActualDeliveryDate = actualDate,
            AlreadyReviewed = alreadyReviewed
        };

        IsReviewModalOpen = true;
        StateHasChanged();
    }

    private void HandleReviewSubmitted(int poItemId)
    {
        ReviewedPoItemIds.Add(poItemId);
        if (ReviewOrderItem != null && ReviewOrderItem.POItemID == poItemId)
        {
            ReviewOrderItem.AlreadyReviewed = true;
        }
        StateHasChanged();
    }

    // =========================================================================
    // DELIVERY / GOODS RECEIVING METHODS (PHASE 2Q-C)
    // =========================================================================
    private void StartRecordDelivery()
    {
        if (SelectedPoForDetail == null) return;

        var firstItem = SelectedPoForDetail.Items?.FirstOrDefault();
        decimal orderedQty = firstItem?.Quantity ?? 0;

        var alreadyReceived = SelectedPoDeliveryRecords?
            .Where(d => d.POItemID == firstItem?.POItemID && !string.Equals(d.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
            .Sum(d => d.ReceivedQuantity) ?? 0;

        decimal remainingQty = Math.Max(0, orderedQty - alreadyReceived);

        DeliveryReceivedQuantity = remainingQty > 0 ? remainingQty : orderedQty;
        DeliverySpoiledQuantity = 0;
        DeliveryActualDate = DateTime.Now;
        DeliveryNotes = string.Empty;
        DeliveryFormError = string.Empty;
        DeliverySuccessMessage = string.Empty;
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

        if (SelectedPoForDetail == null)
        {
            DeliveryFormError = "No active purchase order selected.";
            return;
        }

        var firstItem = SelectedPoForDetail.Items?.FirstOrDefault();
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

        var alreadyReceived = SelectedPoDeliveryRecords?
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
        if (SelectedPoForDetail == null) return;

        DeliveryFormError = string.Empty;
        IsSubmittingDelivery = true;
        StateHasChanged();

        try
        {
            var firstItem = SelectedPoForDetail.Items?.FirstOrDefault();
            int poItemId = firstItem?.POItemID ?? 0;

            // 1. Create delivery record
            var createCmd = new CreateDeliveryRecordCommand
            {
                PurchaseOrderID = SelectedPoForDetail.PurchaseOrderID,
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
            var refreshedPo = await Api.GetPurchaseOrderByIdAsync(SelectedPoForDetail.PurchaseOrderID);
            if (refreshedPo != null)
            {
                if (string.IsNullOrWhiteSpace(refreshedPo.VendorName) && !string.IsNullOrWhiteSpace(SelectedPoForDetail.VendorName))
                {
                    refreshedPo.VendorName = SelectedPoForDetail.VendorName;
                }
                SelectedPoForDetail = refreshedPo;

                // Update in AllPurchaseOrders list as well
                var idx = AllPurchaseOrders.FindIndex(p => p.PurchaseOrderID == refreshedPo.PurchaseOrderID);
                if (idx >= 0)
                {
                    AllPurchaseOrders[idx] = refreshedPo;
                }
            }

            SelectedPoDeliveryRecords = await Api.GetDeliveryRecordsByPurchaseOrderAsync(SelectedPoForDetail.PurchaseOrderID) ?? new List<DeliveryRecordDto>();

            IsRecordingDelivery = false;
            IsReviewingDelivery = false;
            DeliverySuccessMessage = $"Delivery for PO-#{SelectedPoForDetail.PurchaseOrderID} confirmed successfully! Status updated to {SelectedPoForDetail.Status}.";
            ActionSuccessMessage = DeliverySuccessMessage;

            try
            {
                Notifications = await Api.GetMyNotificationsAsync();
            }
            catch { }
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
}
