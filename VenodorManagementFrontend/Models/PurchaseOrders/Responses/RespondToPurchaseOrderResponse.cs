using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class RespondToPurchaseOrderResponse
    {
        public PurchaseOrderDto PurchaseOrder { get; set; } = null!;
    }
