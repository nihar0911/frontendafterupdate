using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class CreateProductCommand
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string Unit { get; set; } = string.Empty;
        public int TaxRateID { get; set; }
        public string Status { get; set; } = "Active";
    }
