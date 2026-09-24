using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetQuotationsResponse
    {
        public List<QuotationDto> Quotations { get; set; } = new();
    }
