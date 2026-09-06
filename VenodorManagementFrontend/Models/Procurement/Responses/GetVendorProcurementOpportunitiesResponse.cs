using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetVendorProcurementOpportunitiesResponse
    {
        public List<VendorProcurementOpportunityDto> Opportunities { get; set; } = new();
    }
