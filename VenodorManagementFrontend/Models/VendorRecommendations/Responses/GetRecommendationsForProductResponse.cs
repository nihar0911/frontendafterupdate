using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetRecommendationsForProductResponse
{
    public int OutletID { get; set; }
    public int ProductID { get; set; }
    public bool HasActiveContracts { get; set; }
    public List<VendorRecommendationDto> Recommendations { get; set; } = new();
}
