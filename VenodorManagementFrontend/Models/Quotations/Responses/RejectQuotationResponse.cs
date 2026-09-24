using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class RejectQuotationResponse
    {
        public QuotationDto Quotation { get; set; } = null!;
    }
