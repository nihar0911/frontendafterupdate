using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class CreateVendorProductCommand
    {
        public int VendorID { get; set; }
        public int ProductID { get; set; }
        public decimal UnitPrice { get; set; }
        public int EstimatedDeliveryDays { get; set; }
        public string Status { get; set; } = "Active";
    }
