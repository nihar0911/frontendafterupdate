using VenodorManagementFrontend.Models.Invoices.DTOs;

namespace VenodorManagementFrontend.Models.Payments.Responses;

public class MarkInvoicePaidResponse
{
    public InvoiceDto Invoice { get; set; } = null!;
}
