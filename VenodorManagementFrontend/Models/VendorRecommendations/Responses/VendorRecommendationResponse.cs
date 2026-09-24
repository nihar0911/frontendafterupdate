using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class VendorRecommendationResponse
    {
        public int PurchaseRequestID { get; set; }
        public List<VendorRecommendationDto> Recommendations { get; set; } = new();
    }
