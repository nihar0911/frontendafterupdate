using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetQuotationsResponse
    {
        public List<QuotationDto> Quotations { get; set; } = new();
    }
