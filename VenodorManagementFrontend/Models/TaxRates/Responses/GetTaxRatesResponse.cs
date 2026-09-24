using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetTaxRatesResponse
    {
        public List<TaxRateDto> TaxRates { get; set; } = new();
    }
