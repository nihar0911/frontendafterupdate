namespace VenodorManagementFrontend.Models.VendorPerformance;

public class CreateVendorReviewRequest
{
    public int VendorID { get; set; }
    public int OutletID { get; set; }
    public int PurchaseOrderID { get; set; }
    public int POItemID { get; set; }
    public int RatedByUserID { get; set; }
    public decimal Rating { get; set; }
    public decimal ProductQualityRating { get; set; }
    public decimal DeliveryRating { get; set; }
    public string Review { get; set; } = string.Empty;
}
