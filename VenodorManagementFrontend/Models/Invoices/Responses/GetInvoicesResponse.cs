using System.Collections.Generic;
using VendorManagement.Web.Models.Invoices.DTOs;

namespace VendorManagement.Web.Models.Invoices.Responses;

public class GetInvoicesResponse
{
    public List<InvoiceDto> Invoices { get; set; } = new();
}