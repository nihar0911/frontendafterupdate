using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class PurchaseRequestItemDto
    {
        public int ItemID { get; set; }
        public int RequestID { get; set; }
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
