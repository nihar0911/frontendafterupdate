using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using VenodorManagementFrontend.Models;
using VenodorManagementFrontend.Models.Invoices.DTOs;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.OrgInvoices;

public partial class OrgInvoices : ComponentBase
{
    [Parameter] public int? InvoiceId { get; set; }

    [Inject] private AuthService Auth { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ApiService Api { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool IsSidebarCollapsed { get; set; } = false;
    private bool IsLoading { get; set; } = true;
    private string OrganizationName { get; set; } = "Organization";
    private string OutletDisplayName { get; set; } = string.Empty;
    private string SearchQuery { get; set; } = string.Empty;
    private string StatusFilter { get; set; } = "All";

    private List<InvoiceDto> AllInvoices { get; set; } = new();
    private InvoiceDto? SelectedInvoice { get; set; }
    private PurchaseOrderDto? SelectedPo { get; set; }
    private List<DeliveryRecordDto> SelectedDeliveryRecords { get; set; } = new();
    private bool IsLoadingDetails { get; set; } = false;
    private bool IsViewingDetails => SelectedInvoice != null;

    // Action state
    private bool IsProcessingAction { get; set; } = false;
    private string ActionError { get; set; } = string.Empty;
    private string ActionSuccess { get; set; } = string.Empty;

    // Reject Modal state
    private bool ShowRejectModal { get; set; } = false;
    private string RejectReason { get; set; } = string.Empty;
    private string RejectError { get; set; } = string.Empty;

    // Summary Metrics
    private int TotalInvoicesCount => AllInvoices.Count;
    private int PendingInvoicesCount => AllInvoices.Count(i => string.Equals(i.Status, "Pending", StringComparison.OrdinalIgnoreCase));
    private int ApprovedInvoicesCount => AllInvoices.Count(i => string.Equals(i.Status, "Approved", StringComparison.OrdinalIgnoreCase));
    private decimal TotalInvoicedAmount => AllInvoices.Sum(i => i.TotalAmount);

    private IEnumerable<InvoiceDto> FilteredInvoices
    {
        get
        {
            var list = AllInvoices.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(StatusFilter) && !string.Equals(StatusFilter, "All", StringComparison.OrdinalIgnoreCase))
            {
                list = list.Where(i => string.Equals(i.Status, StatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                var q = SearchQuery.Trim().ToLowerInvariant();
                list = list.Where(i =>
                    ($"INV-{i.InvoiceID}").ToLowerInvariant().Contains(q) ||
                    ($"PO-{i.PurchaseOrderID}").ToLowerInvariant().Contains(q) ||
                    (!string.IsNullOrEmpty(i.VendorName) && i.VendorName.ToLowerInvariant().Contains(q)) ||
                    (!string.IsNullOrEmpty(i.OutletName) && i.OutletName.ToLowerInvariant().Contains(q))
                );
            }

            return list.OrderByDescending(i => i.InvoiceID);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (InvoiceId.HasValue && InvoiceId.Value > 0)
        {
            await OpenInvoiceDetailsAsync(InvoiceId.Value);
        }
    }

    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            if (Auth.OrganizationID.HasValue && Auth.OrganizationID.Value > 0)
            {
                var org = await Api.GetOrganizationByIdAsync(Auth.OrganizationID.Value);
                if (org != null && !string.IsNullOrWhiteSpace(org.OrganizationName))
                {
                    OrganizationName = org.OrganizationName;
                }
            }

            if ((Auth.IsPurchaseManager || Auth.IsOutletManager) && Auth.OutletID.HasValue && Auth.OutletID.Value > 0)
            {
                try
                {
                    var outlets = await Api.GetOutletsAsync();
                    var myOutlet = outlets?.FirstOrDefault(o => o.OutletID == Auth.OutletID.Value);
                    if (myOutlet != null && !string.IsNullOrWhiteSpace(myOutlet.OutletName))
                    {
                        OutletDisplayName = myOutlet.OutletName;
                    }
                }
                catch { }
            }

            var rawInvoices = await Api.GetInvoicesAsync() ?? new List<InvoiceDto>();
            if ((Auth.IsPurchaseManager || Auth.IsOutletManager) && Auth.OutletID.HasValue && Auth.OutletID.Value > 0)
            {
                AllInvoices = rawInvoices.Where(i => i.OutletID == Auth.OutletID.Value).ToList();
            }
            else
            {
                AllInvoices = rawInvoices;
            }

            if (InvoiceId.HasValue && InvoiceId.Value > 0)
            {
                await OpenInvoiceDetailsAsync(InvoiceId.Value);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrgInvoices] Error loading data: {ex.Message}");
            AllInvoices = new List<InvoiceDto>();
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task OpenInvoiceDetailsAsync(int invoiceId)
    {
        IsLoadingDetails = true;
        ActionError = string.Empty;
        ActionSuccess = string.Empty;
        StateHasChanged();

        try
        {
            var invoice = await Api.GetInvoiceByIdAsync(invoiceId);
            if (invoice != null)
            {
                SelectedInvoice = invoice;

                // Load related PO and delivery records for 3-way match
                var poTask = Api.GetPurchaseOrderByIdAsync(invoice.PurchaseOrderID);
                var drTask = Api.GetDeliveryRecordsByPurchaseOrderAsync(invoice.PurchaseOrderID);
                await Task.WhenAll(poTask, drTask);

                SelectedPo = await poTask;
                SelectedDeliveryRecords = await drTask ?? new List<DeliveryRecordDto>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrgInvoices] Error opening invoice details: {ex.Message}");
        }
        finally
        {
            IsLoadingDetails = false;
            StateHasChanged();
        }
    }

    private void BackToInvoiceList()
    {
        SelectedInvoice = null;
        SelectedPo = null;
        SelectedDeliveryRecords.Clear();
        ActionError = string.Empty;
        ActionSuccess = string.Empty;
        Nav.NavigateTo("/organization/invoices");
    }

    private decimal GetDeliveredQuantity(int productId)
    {
        if (SelectedDeliveryRecords == null || SelectedDeliveryRecords.Count == 0) return 0;
        var poItem = SelectedPo?.Items?.FirstOrDefault(i => i.ProductID == productId);
        if (poItem != null)
        {
            var dr = SelectedDeliveryRecords.FirstOrDefault(d => d.POItemID == poItem.POItemID);
            if (dr != null) return dr.ReceivedQuantity;
        }
        return 0;
    }

    private decimal GetSpoiledQuantity(int productId)
    {
        if (SelectedDeliveryRecords == null || SelectedDeliveryRecords.Count == 0) return 0;
        var poItem = SelectedPo?.Items?.FirstOrDefault(i => i.ProductID == productId);
        if (poItem != null)
        {
            var dr = SelectedDeliveryRecords.FirstOrDefault(d => d.POItemID == poItem.POItemID);
            if (dr != null) return dr.SpoiledQuantity;
        }
        return 0;
    }

    private async Task HandleApproveInvoice()
    {
        if (SelectedInvoice == null) return;
        if (!Auth.IsAdmin && !Auth.IsPurchaseManager)
        {
            ActionError = "You do not have permission to approve invoices.";
            return;
        }

        IsProcessingAction = true;
        ActionError = string.Empty;
        ActionSuccess = string.Empty;

        try
        {
            var result = await Api.ApproveInvoiceAsync(SelectedInvoice.InvoiceID);
            if (result.Success && result.Data != null)
            {
                SelectedInvoice.Status = "Approved";
                ActionSuccess = $"Invoice INV-{SelectedInvoice.InvoiceID} has been successfully approved!";
                AllInvoices = await Api.GetInvoicesAsync() ?? new List<InvoiceDto>();
            }
            else
            {
                ActionError = result.ErrorMessage ?? "Failed to approve invoice.";
            }
        }
        catch (Exception ex)
        {
            ActionError = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessingAction = false;
            StateHasChanged();
        }
    }

    private void OpenRejectModal()
    {
        RejectReason = string.Empty;
        RejectError = string.Empty;
        ShowRejectModal = true;
    }

    private void CloseRejectModal()
    {
        ShowRejectModal = false;
        RejectReason = string.Empty;
        RejectError = string.Empty;
    }

    private async Task HandleConfirmReject()
    {
        if (SelectedInvoice == null) return;
        if (!Auth.IsAdmin && !Auth.IsPurchaseManager)
        {
            RejectError = "You do not have permission to reject invoices.";
            return;
        }
        if (string.IsNullOrWhiteSpace(RejectReason))
        {
            RejectError = "Please enter a reason for rejecting this invoice.";
            return;
        }

        IsProcessingAction = true;
        RejectError = string.Empty;

        try
        {
            var result = await Api.RejectInvoiceAsync(SelectedInvoice.InvoiceID, RejectReason.Trim());
            if (result.Success && result.Data != null)
            {
                SelectedInvoice.Status = "Rejected";
                ActionSuccess = $"Invoice INV-{SelectedInvoice.InvoiceID} has been rejected.";
                CloseRejectModal();
                AllInvoices = await Api.GetInvoicesAsync() ?? new List<InvoiceDto>();
            }
            else
            {
                RejectError = result.ErrorMessage ?? "Failed to reject invoice.";
            }
        }
        catch (Exception ex)
        {
            RejectError = $"Error: {ex.Message}";
        }
        finally
        {
            IsProcessingAction = false;
            StateHasChanged();
        }
    }

    private async Task HandleDownloadPdf(InvoiceDto invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvoiceDocumentBase64))
        {
            var full = await Api.GetInvoiceByIdAsync(invoice.InvoiceID);
            if (full != null && !string.IsNullOrWhiteSpace(full.InvoiceDocumentBase64))
            {
                invoice = full;
            }
        }

        if (!string.IsNullOrWhiteSpace(invoice.InvoiceDocumentBase64))
        {
            string fileName = string.IsNullOrWhiteSpace(invoice.InvoiceFileName) ? $"Invoice-{invoice.InvoiceID}.pdf" : invoice.InvoiceFileName;
            await JS.InvokeVoidAsync("downloadFileFromBase64", fileName, invoice.InvoiceContentType ?? "application/pdf", invoice.InvoiceDocumentBase64);
        }
    }

    private async Task HandleViewPdf(InvoiceDto invoice)
    {
        if (string.IsNullOrWhiteSpace(invoice.InvoiceDocumentBase64))
        {
            var full = await Api.GetInvoiceByIdAsync(invoice.InvoiceID);
            if (full != null && !string.IsNullOrWhiteSpace(full.InvoiceDocumentBase64))
            {
                invoice = full;
            }
        }

        if (!string.IsNullOrWhiteSpace(invoice.InvoiceDocumentBase64))
        {
            await JS.InvokeVoidAsync("viewPdfFromBase64", invoice.InvoiceDocumentBase64);
        }
    }

    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    private void HandleLogout()
    {
        Auth.Logout();
        Nav.NavigateTo("/login");
    }
}