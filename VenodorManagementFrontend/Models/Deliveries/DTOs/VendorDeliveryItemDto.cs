using System;

namespace VendorManagement.Web.Models.Deliveries.DTOs;

public class VendorDeliveryItemDto
{
    public int DeliveryRecordID { get; set; }
    public int PurchaseOrderID { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public int POItemID { get; set; }
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int OutletID { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal SpoiledQuantity { get; set; }
    public decimal SpoilagePercentage { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public string DeliveryStatus { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmedByUserName { get; set; }
}
