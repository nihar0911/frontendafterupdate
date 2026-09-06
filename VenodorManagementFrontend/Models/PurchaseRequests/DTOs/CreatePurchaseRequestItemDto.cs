using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class CreatePurchaseRequestItemDto
    {
        public int ProductID { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
