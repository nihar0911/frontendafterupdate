using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class UpdateTaxRateCommand
    {
        public int TaxRateID { get; set; }
        public string TaxName { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
        public string Status { get; set; } = "Active";
    }
