using System;

namespace VenodorManagementFrontend.Models.VendorPerformance;

public class EligibleReviewOrderDto
{
    public int PurchaseOrderID { get; set; }
    public int POItemID { get; set; }
    public int VendorID { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int OutletID { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime? ActualDeliveryDate { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public bool AlreadyReviewed { get; set; }
}
