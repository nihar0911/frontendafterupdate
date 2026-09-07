using System;

namespace VenodorManagementFrontend.Models;

public class ProcurementItem
{
    public string ItemKey { get; set; } = Guid.NewGuid().ToString();
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 10;
    public string Unit { get; set; } = string.Empty;
    public int VendorID { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int EstimatedDeliveryDays { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalFeedbackCount { get; set; }
    public decimal AverageQualityRating { get; set; }
    public string SmartBadge { get; set; } = string.Empty;
    public bool HasActiveContract { get; set; }
    public decimal RemainingQuantity { get; set; }
}
