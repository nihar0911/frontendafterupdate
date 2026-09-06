using System;

namespace VenodorManagementFrontend.Models.VendorPerformance;

public class VendorReviewDto
{
    public int FeedbackID { get; set; }
    public int VendorID { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public int OutletID { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public int PurchaseOrderID { get; set; }
    public int POItemID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int RatedByUserID { get; set; }
    public string RatedByUserName { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public decimal ProductQualityRating { get; set; }
    public decimal DeliveryRating { get; set; }
    public string Review { get; set; } = string.Empty;
    public DateTime FeedbackDate { get; set; }
}
