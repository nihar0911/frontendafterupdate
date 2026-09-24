using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class DispatchPurchaseOrderCommand
    {
        public int PurchaseOrderID { get; set; }
        public int VendorID { get; set; }
    }
