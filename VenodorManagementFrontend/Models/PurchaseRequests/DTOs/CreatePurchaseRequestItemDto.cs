using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class CreatePurchaseRequestItemDto
    {
        public int ProductID { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int? VendorID { get; set; }
    }
