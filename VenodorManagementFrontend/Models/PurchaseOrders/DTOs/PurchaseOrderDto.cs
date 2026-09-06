using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class PurchaseOrderDto
{
    public int PurchaseOrderID { get; set; }
    public int RequestID { get; set; }
    public int QuotationID { get; set; }
    public int VendorID { get; set; }
    public int OutletID { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public DateTime? DispatchDateTime { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string? DeliveryStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ApproverRole { get; set; }
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
}
