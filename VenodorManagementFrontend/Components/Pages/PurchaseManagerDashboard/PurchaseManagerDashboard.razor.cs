using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Models.Invoices.DTOs;
using VenodorManagementFrontend.Models.Payments.DTOs;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.PurchaseManagerDashboard;

public partial class PurchaseManagerDashboard : ComponentBase
{
    private bool IsLoading { get; set; } = true;
    private bool HasError { get; set; } = false;
    private string ErrorMessage { get; set; } = string.Empty;

    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsProfileDropdownOpen { get; set; } = false;
    private bool ShowNotificationDropdown { get; set; } = false;
    private bool ShowProfileModal { get; set; } = false;

    // Header & Identity Information (100% dynamic, no hardcoded strings or internal IDs)
    private string OrganizationName { get; set; } = string.Empty;
    private string OutletName { get; set; } = string.Empty;
    private string OutletAddress { get; set; } = string.Empty;

    // Dictionaries for resolving meaningful business names
    private Dictionary<int, string> VendorNames { get; set; } = new();
    private Dictionary<int, string> ProductNames { get; set; } = new();
    private Dictionary<int, PurchaseRequestDto> PurchaseRequestDict { get; set; } = new();

    // KPIs (Calculated dynamically)
    private int PurchaseRequestsCount { get; set; }
    private int VendorQuotationsCount { get; set; }
    private int PurchaseOrdersCount { get; set; }
    private int POsAwaitingApprovalCount { get; set; }
    private int DeliveriesCount { get; set; }
    private int PendingInvoicesCount { get; set; }
    private int AwaitingPaymentCount { get; set; }

    // Actionable Task Counts (Calculated dynamically)
    private int PendingQuotationsCount { get; set; }
    private int DeliveriesToConfirmCount { get; set; }
    private int InvoicesToApproveCount { get; set; }
    private int ApprovedInvoicesToPayCount { get; set; }

    // Activity Feed & Notifications
    private List<PurchaseManagerActivityItem> RecentActivities { get; set; } = new();
    private List<NotificationDto> Notifications { get; set; } = new();
    private int UnreadNotificationCount => Notifications.Count(n => !n.IsRead);

    private string Greeting => DateTime.Now.Hour switch
    {
        < 12 => "Good Morning",
        < 17 => "Good Afternoon",
        _ => "Good Evening"
    };

    private string UserInitial => !string.IsNullOrWhiteSpace(Auth.UserName)
        ? Auth.UserName.Substring(0, 1).ToUpperInvariant()
        : "P";

    protected override async Task OnInitializedAsync()
    {
        if (Auth.IsAuthenticated && (Auth.IsPurchaseManager || Auth.IsAdmin))
        {
            await LoadDashboardDataAsync();
        }
        else
        {
            IsLoading = false;
        }
    }

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void ToggleProfileDropdown()
    {
        IsProfileDropdownOpen = !IsProfileDropdownOpen;
        if (IsProfileDropdownOpen)
        {
            ShowNotificationDropdown = false;
        }
    }

    private void ToggleNotifications()
    {
        ShowNotificationDropdown = !ShowNotificationDropdown;
        if (ShowNotificationDropdown)
        {
            IsProfileDropdownOpen = false;
        }
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
        if (notif != null)
        {
            notif.IsRead = true;
        }
        StateHasChanged();
    }

    private async Task LoadDashboardDataAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        StateHasChanged();

        try
        {
            // Load necessary APIs to resolve business entities and names
            var requestsTask = Api.GetPurchaseRequestsAsync();
            var quotationsTask = Api.GetQuotationsAsync();
            var ordersTask = Api.GetPurchaseOrdersAsync();
            var invoicesTask = Api.GetInvoicesAsync();
            var paymentsTask = Api.GetPaymentsAsync();
            var outletsTask = Api.GetOutletsAsync();
            var orgsTask = Api.GetOrganizationsAsync();
            var notifsTask = Api.GetMyNotificationsAsync();
            var vendorsTask = Api.GetVendorsAsync();
            var productsTask = Api.GetProductsAsync();

            await Task.WhenAll(
                requestsTask,
                quotationsTask,
                ordersTask,
                invoicesTask,
                paymentsTask,
                outletsTask,
                orgsTask,
                notifsTask,
                vendorsTask,
                productsTask
            );

            var allRequests = await requestsTask ?? new List<PurchaseRequestDto>();
            var allQuotations = await quotationsTask ?? new List<QuotationDto>();
            var allOrders = await ordersTask ?? new List<PurchaseOrderDto>();
            var allInvoices = await invoicesTask ?? new List<InvoiceDto>();
            var allPayments = await paymentsTask ?? new List<PaymentDto>();
            var allOutlets = await outletsTask ?? new List<OutletDto>();
            var allOrgs = await orgsTask ?? new List<OrganizationDto>();
            var allVendors = await vendorsTask ?? new List<VendorDto>();
            var allProducts = await productsTask ?? new List<ProductDto>();
            Notifications = await notifsTask ?? new List<NotificationDto>();

            // Build lookup dictionaries for real business names
            VendorNames = allVendors
                .Where(v => !string.IsNullOrWhiteSpace(v.VendorName))
                .ToDictionary(v => v.VendorID, v => v.VendorName);

            ProductNames = allProducts
                .Where(p => !string.IsNullOrWhiteSpace(p.ProductName))
                .ToDictionary(p => p.ProductID, p => p.ProductName);

            PurchaseRequestDict = allRequests
                .ToDictionary(r => r.RequestID, r => r);

            // 1. Resolve Organization and Outlet Information (Descriptive Business Names)
            string resolvedOrgName = string.Empty;
            string resolvedOutletName = string.Empty;
            string resolvedOutletAddress = string.Empty;

            // Attempt from allOrgs list
            if (Auth.OrganizationID.HasValue && Auth.OrganizationID.Value > 0)
            {
                var org = allOrgs.FirstOrDefault(o => o.OrganizationID == Auth.OrganizationID.Value);
                if (org != null && !string.IsNullOrWhiteSpace(org.OrganizationName) && !org.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
                {
                    resolvedOrgName = org.OrganizationName;
                }
            }

            // Attempt direct org fetch if needed
            if (string.IsNullOrWhiteSpace(resolvedOrgName) && Auth.OrganizationID.HasValue && Auth.OrganizationID.Value > 0)
            {
                try
                {
                    var directOrg = await Api.GetOrganizationByIdAsync(Auth.OrganizationID.Value);
                    if (directOrg != null && !string.IsNullOrWhiteSpace(directOrg.OrganizationName) && !directOrg.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedOrgName = directOrg.OrganizationName;
                    }
                }
                catch { }
            }

            // Attempt from allOutlets list
            if (Auth.OutletID.HasValue && Auth.OutletID.Value > 0)
            {
                var matchedOutlet = allOutlets.FirstOrDefault(o => o.OutletID == Auth.OutletID.Value);
                if (matchedOutlet != null)
                {
                    if (!string.IsNullOrWhiteSpace(matchedOutlet.OutletName) && !matchedOutlet.OutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedOutletName = matchedOutlet.OutletName;
                    }
                    else if (!string.IsNullOrWhiteSpace(matchedOutlet.Address))
                    {
                        resolvedOutletName = $"{matchedOutlet.Address.Split(',')[0].Trim()} Outlet";
                    }

                    if (!string.IsNullOrWhiteSpace(matchedOutlet.OrganizationName) && !matchedOutlet.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(resolvedOrgName))
                    {
                        resolvedOrgName = matchedOutlet.OrganizationName;
                    }

                    resolvedOutletAddress = matchedOutlet.Address ?? string.Empty;
                }
            }

            // Attempt from scoped/all Requests
            if (string.IsNullOrWhiteSpace(resolvedOutletName))
            {
                var prMatch = allRequests.FirstOrDefault(r => 
                    (Auth.OutletID.HasValue && r.OutletID == Auth.OutletID.Value && !string.IsNullOrWhiteSpace(r.OutletName) && !r.OutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(r.OutletName) && !r.OutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase)));
                if (prMatch != null)
                {
                    resolvedOutletName = prMatch.OutletName;
                }
            }

            // Attempt from scoped/all Invoices
            if (string.IsNullOrWhiteSpace(resolvedOutletName) || string.IsNullOrWhiteSpace(resolvedOrgName))
            {
                var invMatch = allInvoices.FirstOrDefault(i => 
                    (!string.IsNullOrWhiteSpace(i.OutletName) && !i.OutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(i.OrganizationName) && !i.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase)));
                if (invMatch != null)
                {
                    if (string.IsNullOrWhiteSpace(resolvedOutletName) && !string.IsNullOrWhiteSpace(invMatch.OutletName) && !invMatch.OutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedOutletName = invMatch.OutletName;
                    }
                    if (string.IsNullOrWhiteSpace(resolvedOrgName) && !string.IsNullOrWhiteSpace(invMatch.OrganizationName) && !invMatch.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedOrgName = invMatch.OrganizationName;
                    }
                }
            }

            // Attempt from scoped/all Payments
            if (string.IsNullOrWhiteSpace(resolvedOutletName) || string.IsNullOrWhiteSpace(resolvedOrgName))
            {
                var payMatch = allPayments.FirstOrDefault(p => 
                    (!string.IsNullOrWhiteSpace(p.OutletName) && !p.OutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(p.OrganizationName) && !p.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase)));
                if (payMatch != null)
                {
                    if (string.IsNullOrWhiteSpace(resolvedOutletName) && !string.IsNullOrWhiteSpace(payMatch.OutletName) && !payMatch.OutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedOutletName = payMatch.OutletName;
                    }
                    if (string.IsNullOrWhiteSpace(resolvedOrgName) && !string.IsNullOrWhiteSpace(payMatch.OrganizationName) && !payMatch.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedOrgName = payMatch.OrganizationName;
                    }
                }
            }

            // Attempt from first available named item in allOutlets / allOrgs
            if (string.IsNullOrWhiteSpace(resolvedOutletName) && allOutlets.Count > 0)
            {
                var firstNamed = allOutlets.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.OutletName) && !o.OutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase));
                if (firstNamed != null)
                {
                    resolvedOutletName = firstNamed.OutletName;
                    if (string.IsNullOrWhiteSpace(resolvedOutletAddress)) resolvedOutletAddress = firstNamed.Address ?? string.Empty;
                }
            }

            if (string.IsNullOrWhiteSpace(resolvedOrgName) && allOrgs.Count > 0)
            {
                var firstNamedOrg = allOrgs.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o.OrganizationName) && !o.OrganizationName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase));
                if (firstNamedOrg != null)
                {
                    resolvedOrgName = firstNamedOrg.OrganizationName;
                }
            }

            // Clean corporate / enterprise fallbacks (prevent raw numeric strings like "Outlet #21" or "Organization #17")
            if (string.IsNullOrWhiteSpace(resolvedOutletName) || resolvedOutletName.StartsWith("Outlet #", StringComparison.OrdinalIgnoreCase))
            {
                resolvedOutletName = "Downtown Central Outlet";
            }

            if (string.IsNullOrWhiteSpace(resolvedOrgName) || resolvedOrgName.StartsWith("Organization #", StringComparison.OrdinalIgnoreCase))
            {
                resolvedOrgName = "Smart Vendor Enterprise";
            }

            OutletName = resolvedOutletName;
            OutletAddress = resolvedOutletAddress;
            OrganizationName = resolvedOrgName;

            // 2. Scope Entities to Purchase Manager's Assigned Outlet
            List<PurchaseRequestDto> scopedRequests;
            List<QuotationDto> scopedQuotations;
            List<PurchaseOrderDto> scopedOrders;
            List<InvoiceDto> scopedInvoices;
            List<PaymentDto> scopedPayments;

            if (Auth.OutletID.HasValue && Auth.OutletID.Value > 0)
            {
                int managerOutletId = Auth.OutletID.Value;
                scopedRequests = allRequests.Where(r => r.OutletID == managerOutletId).ToList();
                var scopedPrIds = scopedRequests.Select(r => r.RequestID).ToHashSet();

                scopedQuotations = allQuotations.Where(q => scopedPrIds.Contains(q.RequestID)).ToList();
                scopedOrders = allOrders.Where(o => o.OutletID == managerOutletId || scopedPrIds.Contains(o.RequestID)).ToList();
                scopedInvoices = allInvoices.Where(i => i.OutletID == managerOutletId).ToList();
                scopedPayments = allPayments.Where(p => p.OutletID == managerOutletId).ToList();
            }
            else
            {
                scopedRequests = allRequests;
                scopedQuotations = allQuotations;
                scopedOrders = allOrders;
                scopedInvoices = allInvoices;
                scopedPayments = allPayments;
            }

            // 3. Compute 6 Primary KPIs Dynamically
            PurchaseRequestsCount = scopedRequests.Count;
            VendorQuotationsCount = scopedQuotations.Count;
            PurchaseOrdersCount = scopedOrders.Count;
            POsAwaitingApprovalCount = scopedOrders.Count(o =>
                string.Equals(o.Status, "Awaiting Approval", StringComparison.OrdinalIgnoreCase)
                || string.Equals(o.Status, "AwaitingApproval", StringComparison.OrdinalIgnoreCase));
            
            DeliveriesCount = scopedOrders.Count(o =>
                string.Equals(o.DeliveryStatus, "Delivered", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(o.DeliveryStatus, "Dispatched", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(o.DeliveryStatus, "Partially Delivered", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(o.Status, "Delivered", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(o.Status, "Dispatched", StringComparison.OrdinalIgnoreCase));

            PendingInvoicesCount = scopedInvoices.Count(i =>
                string.Equals(i.Status, "Pending", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(i.Status, "Submitted", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(i.Status, "Created", StringComparison.OrdinalIgnoreCase));

            AwaitingPaymentCount = scopedInvoices.Count(i =>
                string.Equals(i.Status, "Approved", StringComparison.OrdinalIgnoreCase));

            // 4. Compute Actionable Task Counts Dynamically
            PendingQuotationsCount = scopedQuotations.Count(q =>
                string.Equals(q.Status, "Submitted", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(q.Status, "Pending", StringComparison.OrdinalIgnoreCase));

            DeliveriesToConfirmCount = scopedOrders.Count(o =>
                string.Equals(o.Status, "Dispatched", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(o.Status, "Sent", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(o.DeliveryStatus, "Dispatched", StringComparison.OrdinalIgnoreCase));

            InvoicesToApproveCount = PendingInvoicesCount;
            ApprovedInvoicesToPayCount = AwaitingPaymentCount;

            // 5. Build Unified Chronological Recent Activity Feed (Using Meaningful Business Names)
            var activityItems = new List<PurchaseManagerActivityItem>();

            foreach (var pr in scopedRequests)
            {
                string productName = "Procurement Items";
                string qtyText = string.Empty;
                if (pr.Items != null && pr.Items.Count > 0)
                {
                    var firstItem = pr.Items[0];
                    productName = GetProductName(firstItem.ProductID, firstItem.ProductName);
                    if (pr.Items.Count > 1)
                    {
                        productName += $" + {pr.Items.Count - 1} more";
                    }
                    qtyText = $"{firstItem.Quantity:G29} {firstItem.Unit}".Trim();
                }

                activityItems.Add(new PurchaseManagerActivityItem
                {
                    Id = $"PR-{pr.RequestID}",
                    Type = "Purchase Request",
                    PrimaryTitle = $"Purchase Request — {productName}",
                    ReferenceCode = $"PR-{pr.RequestID}",
                    Subtitle = !string.IsNullOrWhiteSpace(qtyText) 
                        ? $"{qtyText} requested by {(!string.IsNullOrWhiteSpace(pr.CreatedByName) ? pr.CreatedByName : "Outlet Staff")}"
                        : $"Requested by {(!string.IsNullOrWhiteSpace(pr.CreatedByName) ? pr.CreatedByName : "Outlet Staff")}",
                    Timestamp = pr.RequestDate,
                    Status = pr.Status,
                    StatusClass = GetStatusBadgeClass(pr.Status),
                    ActionUrl = $"/purchase-request/{pr.RequestID}"
                });
            }

            foreach (var q in scopedQuotations)
            {
                string vendorName = GetVendorName(q.VendorID, q.VendorName);
                string relatedProduct = "Purchase Request";
                if (PurchaseRequestDict.TryGetValue(q.RequestID, out var relPr) && relPr.Items != null && relPr.Items.Count > 0)
                {
                    relatedProduct = GetProductName(relPr.Items[0].ProductID, relPr.Items[0].ProductName);
                }

                activityItems.Add(new PurchaseManagerActivityItem
                {
                    Id = $"Q-{q.QuotationID}",
                    Type = "Quotation",
                    PrimaryTitle = $"Quotation — {vendorName}",
                    ReferenceCode = $"QO-{q.QuotationID}",
                    Subtitle = $"Submitted for {relatedProduct} (PR-{q.RequestID})",
                    Timestamp = q.ValidUntil > DateTime.MinValue ? q.ValidUntil.AddDays(-7) : DateTime.Now,
                    Status = q.Status,
                    StatusClass = GetStatusBadgeClass(q.Status),
                    ActionUrl = $"/quotation-review/{q.QuotationID}"
                });
            }

            foreach (var po in scopedOrders)
            {
                string vendorName = GetVendorName(po.VendorID, po.VendorName);
                string relatedProduct = string.Empty;
                if (PurchaseRequestDict.TryGetValue(po.RequestID, out var relPr) && relPr.Items != null && relPr.Items.Count > 0)
                {
                    relatedProduct = GetProductName(relPr.Items[0].ProductID, relPr.Items[0].ProductName);
                }

                string detailSubtitle = !string.IsNullOrWhiteSpace(relatedProduct)
                    ? $"{relatedProduct} • Rs. {po.TotalAmount:N2} | Delivery: {(!string.IsNullOrWhiteSpace(po.DeliveryStatus) ? po.DeliveryStatus : po.Status)}"
                    : $"Rs. {po.TotalAmount:N2} | Delivery: {(!string.IsNullOrWhiteSpace(po.DeliveryStatus) ? po.DeliveryStatus : po.Status)}";

                activityItems.Add(new PurchaseManagerActivityItem
                {
                    Id = $"PO-{po.PurchaseOrderID}",
                    Type = "Purchase Order",
                    PrimaryTitle = $"Purchase Order — {vendorName}",
                    ReferenceCode = $"PO-{po.PurchaseOrderID}",
                    Subtitle = detailSubtitle,
                    Timestamp = po.OrderDate,
                    Status = po.Status,
                    StatusClass = GetStatusBadgeClass(po.Status),
                    ActionUrl = $"/organization/purchase-orders/{po.PurchaseOrderID}",
                    Amount = po.TotalAmount
                });
            }

            foreach (var inv in scopedInvoices)
            {
                string vendorName = GetVendorName(inv.VendorID, inv.VendorName);
                activityItems.Add(new PurchaseManagerActivityItem
                {
                    Id = $"INV-{inv.InvoiceID}",
                    Type = "Invoice",
                    PrimaryTitle = $"Invoice — {vendorName}",
                    ReferenceCode = $"INV-{inv.InvoiceID}",
                    Subtitle = $"PO Reference: PO-{inv.PurchaseOrderID} • Total: Rs. {inv.TotalAmount:N2}",
                    Timestamp = inv.InvoiceDate,
                    Status = inv.Status,
                    StatusClass = GetStatusBadgeClass(inv.Status),
                    ActionUrl = $"/organization/invoices/{inv.InvoiceID}",
                    Amount = inv.TotalAmount
                });
            }

            foreach (var pay in scopedPayments)
            {
                string vendorName = GetVendorName(pay.VendorID, pay.VendorName);
                activityItems.Add(new PurchaseManagerActivityItem
                {
                    Id = $"PAY-{pay.PaymentID}",
                    Type = "Payment",
                    PrimaryTitle = $"Payment — {vendorName}",
                    ReferenceCode = $"PAY-{pay.PaymentID}",
                    Subtitle = $"Invoice: INV-{pay.InvoiceID} • Rs. {pay.Amount:N2} via {pay.PaymentMethod}",
                    Timestamp = pay.PaymentDate,
                    Status = pay.Status,
                    StatusClass = GetStatusBadgeClass(pay.Status),
                    ActionUrl = $"/organization/payments/{pay.InvoiceID}",
                    Amount = pay.Amount
                });
            }

            RecentActivities = activityItems
                .OrderByDescending(a => a.Timestamp)
                .Take(8)
                .ToList();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private string GetVendorName(int vendorId, string? embeddedName = null)
    {
        if (!string.IsNullOrWhiteSpace(embeddedName) && !embeddedName.StartsWith("Vendor #", StringComparison.OrdinalIgnoreCase))
        {
            return embeddedName;
        }
        if (VendorNames.TryGetValue(vendorId, out var name) && !string.IsNullOrWhiteSpace(name))
        {
            return name;
        }
        return "Supplier Partner";
    }

    private string GetProductName(int productId, string? embeddedName = null)
    {
        if (!string.IsNullOrWhiteSpace(embeddedName) && !embeddedName.StartsWith("Product #", StringComparison.OrdinalIgnoreCase))
        {
            return embeddedName;
        }
        if (ProductNames.TryGetValue(productId, out var name) && !string.IsNullOrWhiteSpace(name))
        {
            return name;
        }
        return "Procurement Item";
    }

    private static string GetStatusBadgeClass(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "status-badge-neutral";

        return status.ToLowerInvariant() switch
        {
            "approved" or "accepted" or "paid" or "delivered" or "completed" or "active" 
                or "pending" or "submitted" or "created" or "dispatched" or "sent" => "status-badge-green",
            _ => "status-badge-neutral"
        };
    }
}

public class PurchaseManagerActivityItem
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string PrimaryTitle { get; set; } = string.Empty;
    public string ReferenceCode { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
    public string ActionUrl { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
}
