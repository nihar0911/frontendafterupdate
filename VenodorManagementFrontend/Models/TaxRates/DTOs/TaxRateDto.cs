using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class TaxRateDto
    {
        public int TaxRateID { get; set; }
        public string TaxName { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
