using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetQuotationByIdResponse
    {
        public QuotationDto Quotation { get; set; } = null!;
    }
