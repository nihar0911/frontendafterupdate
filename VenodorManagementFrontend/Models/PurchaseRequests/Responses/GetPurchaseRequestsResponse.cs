using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetPurchaseRequestsResponse
    {
        public List<PurchaseRequestDto> PurchaseRequests { get; set; } = new();
    }
