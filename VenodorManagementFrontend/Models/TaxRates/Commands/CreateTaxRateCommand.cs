using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class CreateTaxRateCommand
    {
        public string TaxName { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
        public string Status { get; set; } = "Active";
    }
