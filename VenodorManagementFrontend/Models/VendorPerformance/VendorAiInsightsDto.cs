using System.Collections.Generic;

namespace VenodorManagementFrontend.Models.VendorPerformance;

public class VendorAiInsightsDto
{
    public string OverallSentiment { get; set; } = "Positive";
    public int SentimentScore { get; set; } = 85;
    public List<string> KeyStrengths { get; set; } = new();
    public List<string> RiskFlags { get; set; } = new();
    public string ExecutiveSummary { get; set; } = string.Empty;
    public int TotalReviewsAnalyzed { get; set; }
}
