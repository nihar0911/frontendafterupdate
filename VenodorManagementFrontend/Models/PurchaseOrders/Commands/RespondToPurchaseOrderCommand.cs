using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class RespondToPurchaseOrderCommand
    {
        public int PurchaseOrderID { get; set; }
        public int VendorID { get; set; }
        public string Status { get; set; } = "Accepted";
    }
