using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetQuotationByIdResponse
    {
        public QuotationDto Quotation { get; set; } = null!;
    }
