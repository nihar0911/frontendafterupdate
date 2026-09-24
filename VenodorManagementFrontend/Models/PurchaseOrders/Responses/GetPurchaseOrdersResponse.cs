using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetPurchaseOrdersResponse
    {
        public List<PurchaseOrderDto> PurchaseOrders { get; set; } = new();
    }
