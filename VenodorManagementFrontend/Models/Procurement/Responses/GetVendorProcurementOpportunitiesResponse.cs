using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetVendorProcurementOpportunitiesResponse
    {
        public List<VendorProcurementOpportunityDto> Opportunities { get; set; } = new();
    }
