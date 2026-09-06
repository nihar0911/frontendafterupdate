using System.Collections.Generic;
using VenodorManagementFrontend.Models.Invoices.DTOs;

namespace VenodorManagementFrontend.Models.Invoices.Responses;

public class GetInvoicesResponse
{
    public List<InvoiceDto> Invoices { get; set; } = new();
}