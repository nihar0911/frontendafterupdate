using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class CreateContractFromQuotationCommand
    {
        public int QuotationID { get; set; }
        public List<CreateContractVendorAllocationDto>? Allocations { get; set; }
    }
