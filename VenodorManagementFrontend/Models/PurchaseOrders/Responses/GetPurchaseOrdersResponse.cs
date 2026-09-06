using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetPurchaseOrdersResponse
    {
        public List<PurchaseOrderDto> PurchaseOrders { get; set; } = new();
    }
