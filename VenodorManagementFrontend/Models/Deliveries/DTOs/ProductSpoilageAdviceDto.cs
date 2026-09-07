using System;

namespace VenodorManagementFrontend.Models;

public class ProductSpoilageAdviceDto
{
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int POItemID { get; set; }
    public decimal CurrentQuantity { get; set; }
    public int DeliveryCount { get; set; }
    public decimal TotalOrderedQuantity { get; set; }
    public decimal TotalReceivedQuantity { get; set; }
    public decimal TotalSpoiledQuantity { get; set; }
    public decimal? WeightedSpoilagePercentage { get; set; }
    public decimal? AverageSpoilagePercentage { get; set; }
    public decimal? RecentSpoilagePercentage { get; set; }
    public decimal? MinimumSpoilagePercentage { get; set; }
    public decimal? MaximumSpoilagePercentage { get; set; }
    public string Trend { get; set; } = "None";
    public string RiskLevel { get; set; } = "INSUFFICIENT DATA";
    public int? PriorityRank { get; set; }
    public decimal? EstimatedSpoiledQuantity { get; set; }
    public string Why { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}
