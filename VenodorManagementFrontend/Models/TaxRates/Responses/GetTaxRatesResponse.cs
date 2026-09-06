using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetTaxRatesResponse
    {
        public List<TaxRateDto> TaxRates { get; set; } = new();
    }
