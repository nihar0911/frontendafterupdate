using System;

namespace VenodorManagementFrontend.Models.VendorPerformance;

public class VendorPerformanceSummaryDto
{
    public int VendorID { get; set; }
    public string VendorName { get; set; } = string.Empty;

    // Delivery Performance
    public int TotalPurchaseOrders { get; set; }
    public int CompletedDeliveries { get; set; }
    public int OnTimeDeliveries { get; set; }
    public int DelayedDeliveries { get; set; }
    public decimal? OnTimeDeliveryRate { get; set; }
    public decimal? AverageDelayDays { get; set; }

    // Quality / Spoilage Performance
    public decimal TotalReceivedQuantity { get; set; }
    public decimal TotalSpoiledQuantity { get; set; }
    public decimal NetAcceptedQuantity { get; set; }
    public decimal? SpoilageRate { get; set; }

    // Invoice Performance
    public int TotalInvoices { get; set; }
    public int ApprovedInvoices { get; set; }
    public int RejectedInvoices { get; set; }
    public decimal? InvoiceApprovalRate { get; set; }

    // Reviews & Feedback
    public int TotalReviews { get; set; }
    public decimal? AverageRating { get; set; }
    public decimal? AverageQualityRating { get; set; }
    public decimal? AverageDeliveryRating { get; set; }

    // Flags for UI state
    public bool HasDeliveryHistory => CompletedDeliveries > 0;
    public bool HasQualityHistory => TotalReceivedQuantity > 0;
    public bool HasInvoiceHistory => TotalInvoices > 0;
    public bool HasReviewHistory => TotalReviews > 0;
}
