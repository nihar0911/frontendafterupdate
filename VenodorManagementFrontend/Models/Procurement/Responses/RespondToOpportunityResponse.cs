using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class RespondToOpportunityResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string OpportunityStatus { get; set; } = string.Empty;
        public int RequestID { get; set; }
        public int VendorID { get; set; }
        public int ProductID { get; set; }
    }
