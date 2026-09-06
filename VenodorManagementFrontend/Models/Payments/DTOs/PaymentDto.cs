using System;

namespace VenodorManagementFrontend.Models.Payments.DTOs;

public class PaymentDto
{
    public int PaymentID { get; set; }
    public int InvoiceID { get; set; }
    public int PurchaseOrderID { get; set; }
    public int VendorID { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int OutletID { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public int OrganizationID { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public string Status { get; set; } = "Paid";
    public string InvoiceStatus { get; set; } = "Paid";
}
