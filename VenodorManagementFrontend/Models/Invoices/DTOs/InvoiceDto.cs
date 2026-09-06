using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models.Invoices.DTOs;

public class InvoiceDto
{
    public int InvoiceID { get; set; }
    public int PurchaseOrderID { get; set; }
    public int VendorID { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int OutletID { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? InvoiceDocumentBase64 { get; set; }
    public string? InvoiceFileName { get; set; }
    public string? InvoiceContentType { get; set; }

    public List<InvoiceItemDto> Items { get; set; } = new();
}