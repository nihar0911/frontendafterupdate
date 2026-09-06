using VenodorManagementFrontend.Models.Invoices.DTOs;

namespace VenodorManagementFrontend.Models.Invoices.Responses;

public class GetInvoiceByIdResponse
{
    public InvoiceDto? Invoice { get; set; }
}