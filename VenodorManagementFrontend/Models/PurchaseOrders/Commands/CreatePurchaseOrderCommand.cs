using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class CreatePurchaseOrderCommand
{
    public int QuotationID  { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; } = DateTime.Now.AddDays(3);
    public string? ApproverRole { get; set; } = "Organization Manager";
}
