using VendorManagement.Web.Models.Invoices.DTOs;

namespace VendorManagement.Web.Models.Payments.Responses;

public class MarkInvoicePaidResponse
{
    public InvoiceDto Invoice { get; set; } = null!;
}
