using System;

namespace VenodorManagementFrontend.Models.VendorRecommendationSettings
{
    public class VendorRecommendationSettingsDto
    {
        public int Id { get; set; }

        public decimal QualityWeight { get; set; } = 35.0m;

        public decimal DeliveryWeight { get; set; } = 25.0m;

        public decimal PriceWeight { get; set; } = 25.0m;

        public decimal ReliabilityWeight { get; set; } = 15.0m;

        public decimal ReliabilityPointsPerReview { get; set; } = 3.0m;

        public decimal NeutralScoreForNewVendors { get; set; } = 70.0m;

        public decimal BestQualityThreshold { get; set; } = 4.5m;

        public decimal FastestDeliveryThreshold { get; set; } = 4.5m;

        public decimal HighQualityRationaleThreshold { get; set; } = 4.0m;

        public bool PrioritizeActiveContracts { get; set; } = true;

        public DateTime? UpdatedAt { get; set; }
    }
}
