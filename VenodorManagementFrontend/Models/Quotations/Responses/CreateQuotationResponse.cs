using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class CreateQuotationResponse
    {
        public QuotationDto Quotation { get; set; } = null!;
    }
