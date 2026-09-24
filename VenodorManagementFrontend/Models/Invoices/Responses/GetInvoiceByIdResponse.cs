using VendorManagement.Web.Models.Invoices.DTOs;

namespace VendorManagement.Web.Models.Invoices.Responses;

public class GetInvoiceByIdResponse
{
    public InvoiceDto? Invoice { get; set; }
}