using System;

namespace VenodorManagementFrontend.Models.Payments.Commands;

public class MarkInvoicePaidCommand
{
    public int InvoiceID { get; set; }
    public int PaidByUserID { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public string? TransactionReference { get; set; }
}
