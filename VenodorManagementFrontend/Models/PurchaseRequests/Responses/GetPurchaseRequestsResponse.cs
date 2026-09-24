using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetPurchaseRequestsResponse
    {
        public List<PurchaseRequestDto> PurchaseRequests { get; set; } = new();
    }
