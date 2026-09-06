using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class DispatchPurchaseOrderCommand
    {
        public int PurchaseOrderID { get; set; }
        public int VendorID { get; set; }
    }
