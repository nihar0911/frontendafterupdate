using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class VendorRecommendationResponse
    {
        public int PurchaseRequestID { get; set; }
        public List<VendorRecommendationDto> Recommendations { get; set; } = new();
    }
