using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using VenodorManagementFrontend.Models.Invoices.DTOs;
using VenodorManagementFrontend.Models.Payments.Commands;
using VenodorManagementFrontend.Models.Payments.DTOs;
using VenodorManagementFrontend.Services;

namespace VenodorManagementFrontend.Components.Pages.OrgPayments;

public partial class OrgPayments : ComponentBase
{
    [Inject] private ApiService Api { get; set; } = null!;
    [Inject] private AuthService Auth { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter] public int? InvoiceId { get; set; }

    protected bool IsLoading { get; set; } = true;
    protected bool IsSubmittingPayment { get; set; } = false;
    protected bool IsSidebarCollapsed { get; set; } = false;
    protected string OrganizationName { get; set; } = "Organization";
    protected string OutletDisplayName { get; set; } = string.Empty;

    protected List<PaymentDto> Payments { get; set; } = new();
    protected List<InvoiceDto> Invoices { get; set; } = new();

    // Tabs: "history" (completed payments) or "pending" (approved invoices awaiting payment)
    protected string ActiveTab { get; set; } = "history";
    protected string SearchTerm { get; set; } = string.Empty;
    protected string MethodFilter { get; set; } = "All";

    // Modal state for recording payment
    protected bool ShowMakePaymentModal { get; set; } = false;
    protected InvoiceDto? SelectedInvoiceForPayment { get; set; }
    protected string SelectedPaymentMethod { get; set; } = "Bank Transfer";
    protected DateTime PaymentDate { get; set; } = DateTime.Today;
    protected string TransactionReference { get; set; } = string.Empty;
    protected string? PaymentErrorMessage { get; set; }
    protected string? PaymentSuccessMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (!Auth.IsAuthenticated || (!Auth.IsOrgManager && !Auth.IsAdmin && !Auth.IsPurchaseManager))
        {
            IsLoading = false;
            return;
        }

        if (Auth.OrganizationID.HasValue)
        {
            try
            {
                var org = await Api.GetOrganizationByIdAsync(Auth.OrganizationID.Value);
                if (org != null && !string.IsNullOrWhiteSpace(org.OrganizationName))
                {
                    OrganizationName = org.OrganizationName;
                }
            }
            catch { }
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

        await LoadDataAsync();

        if (InvoiceId.HasValue && InvoiceId.Value > 0 && (Auth.IsAdmin || Auth.IsPurchaseManager))
        {
            var target = Invoices.FirstOrDefault(i => i.InvoiceID == InvoiceId.Value);
            if (target != null && string.Equals(target.Status, "Approved", StringComparison.OrdinalIgnoreCase))
            {
                OpenMakePaymentModal(target);
            }
        }
    }

    protected async Task LoadDataAsync()
    {
        IsLoading = true;
        PaymentSuccessMessage = null;
        try
        {
            var paymentsTask = Api.GetPaymentsAsync();
            var invoicesTask = Api.GetInvoicesAsync();

            await Task.WhenAll(paymentsTask, invoicesTask);

            var rawPayments = await paymentsTask ?? new List<PaymentDto>();
            var rawInvoices = await invoicesTask ?? new List<InvoiceDto>();

            if ((Auth.IsPurchaseManager || Auth.IsOutletManager) && Auth.OutletID.HasValue && Auth.OutletID.Value > 0)
            {
                Payments = rawPayments.Where(p => p.OutletID == Auth.OutletID.Value).ToList();
                Invoices = rawInvoices.Where(i => i.OutletID == Auth.OutletID.Value).ToList();
            }
            else
            {
                Payments = rawPayments;
                Invoices = rawInvoices;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrgPayments] LoadDataAsync error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }

    protected void SetActiveTab(string tab)
    {
        ActiveTab = tab;
    }

    // Filtered Payments History
    protected IEnumerable<PaymentDto> FilteredPayments
    {
        get
        {
            var query = Payments.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(MethodFilter) && MethodFilter != "All")
            {
                query = query.Where(p => string.Equals(p.PaymentMethod, MethodFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLowerInvariant();
                query = query.Where(p =>
                    $"pay-{p.PaymentID}".Contains(term) ||
                    $"inv-{p.InvoiceID}".Contains(term) ||
                    $"po-{p.PurchaseOrderID}".Contains(term) ||
                    (p.VendorName != null && p.VendorName.ToLowerInvariant().Contains(term)) ||
                    (p.OutletName != null && p.OutletName.ToLowerInvariant().Contains(term)) ||
                    (p.TransactionReference != null && p.TransactionReference.ToLowerInvariant().Contains(term)));
            }

            return query.OrderByDescending(p => p.PaymentDate);
        }
    }

    // Approved unpaid invoices awaiting payment
    protected IEnumerable<InvoiceDto> InvoicesAwaitingPayment
    {
        get
        {
            var query = Invoices.Where(i => string.Equals(i.Status, "Approved", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim().ToLowerInvariant();
                query = query.Where(i =>
                    $"inv-{i.InvoiceID}".Contains(term) ||
                    $"po-{i.PurchaseOrderID}".Contains(term) ||
                    (i.VendorName != null && i.VendorName.ToLowerInvariant().Contains(term)) ||
                    (i.OutletName != null && i.OutletName.ToLowerInvariant().Contains(term)));
            }

            return query.OrderByDescending(i => i.InvoiceDate);
        }
    }

    // Metrics
    protected int TotalPaymentsCount => Payments.Count;
    protected decimal TotalPaidAmount => Payments.Sum(p => p.Amount);
    protected int AwaitingPaymentCount => Invoices.Count(i => string.Equals(i.Status, "Approved", StringComparison.OrdinalIgnoreCase));

    protected void OpenMakePaymentModal(InvoiceDto invoice)
    {
        if (!Auth.IsAdmin && !Auth.IsPurchaseManager)
        {
            return;
        }

        SelectedInvoiceForPayment = invoice;
        SelectedPaymentMethod = "Bank Transfer";
        PaymentDate = DateTime.Today;
        TransactionReference = string.Empty;
        PaymentErrorMessage = null;
        ShowMakePaymentModal = true;
    }

    protected void CloseMakePaymentModal()
    {
        ShowMakePaymentModal = false;
        SelectedInvoiceForPayment = null;
        PaymentErrorMessage = null;
    }

    protected async Task SubmitPaymentAsync()
    {
        if (SelectedInvoiceForPayment == null) return;

        if (!Auth.IsAdmin && !Auth.IsPurchaseManager)
        {
            PaymentErrorMessage = "You do not have permission to record payments.";
            return;
        }

        PaymentErrorMessage = null;
        IsSubmittingPayment = true;

        try
        {
            var command = new MarkInvoicePaidCommand
            {
                InvoiceID = SelectedInvoiceForPayment.InvoiceID,
                PaidByUserID = Auth.CurrentUser?.UserID ?? Auth.UserID,
                PaymentMethod = SelectedPaymentMethod,
                PaymentDate = PaymentDate,
                TransactionReference = TransactionReference
            };

            var result = await Api.PayInvoiceAsync(command);

            if (result != null && result.Success)
            {
                int paidInvoiceId = SelectedInvoiceForPayment.InvoiceID;
                decimal amountPaid = SelectedInvoiceForPayment.TotalAmount;
                CloseMakePaymentModal();

                PaymentSuccessMessage = $"Payment of Rs. {amountPaid:N2} for Invoice INV-{paidInvoiceId} was successfully recorded.";
                await LoadDataAsync();
                ActiveTab = "history";
            }
            else
            {
                PaymentErrorMessage = result?.ErrorMessage ?? "Payment failed. Please verify that the invoice is approved and not already paid.";
            }
        }
        catch (Exception ex)
        {
            PaymentErrorMessage = $"An error occurred while processing payment: {ex.Message}";
        }
        finally
        {
            IsSubmittingPayment = false;
        }
    }

    protected async Task DownloadInvoicePdf(int invoiceId)
    {
        try
        {
            var bytes = await Api.DownloadInvoicePdfAsync(invoiceId);
            if (bytes != null && bytes.Length > 0)
            {
                var base64 = Convert.ToBase64String(bytes);
                await JS.InvokeVoidAsync("downloadFileFromBase64", $"Invoice-{invoiceId}.pdf", "application/pdf", base64);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrgPayments] DownloadInvoicePdf error: {ex.Message}");
        }
    }
}
