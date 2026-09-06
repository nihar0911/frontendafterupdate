using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class VendorRecommendationDto
    {
        public int VendorID { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int EstimatedDeliveryDays { get; set; }
        public decimal OverallScore { get; set; }
        public decimal AverageRating { get; set; }
        public decimal AverageQualityRating { get; set; }
        public decimal AverageDeliveryRating { get; set; }
        public int TotalFeedbackCount { get; set; }
        public int TotalComplaintCount { get; set; }
        public decimal DeliveryCompletionPercentage { get; set; }
        public int CompletedDeliveries { get; set; }
        public decimal? SpoilageRate { get; set; }
        public string Recommendation { get; set; } = string.Empty;
        public string SmartBadge { get; set; } = string.Empty;
        public int Rank { get; set; }
        public bool HasActiveContract { get; set; }
        public int? ContractID { get; set; }
        public decimal AllocationPercentage { get; set; }
        public decimal AllocatedQuantity { get; set; }
        public decimal UsedQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
    }
