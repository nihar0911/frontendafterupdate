using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class CreatePurchaseOrderResponse
    {
        public PurchaseOrderDto PurchaseOrder { get; set; } = null!;
    }
