using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class CreateContractVendorAllocationDto
    {
        public int VendorID { get; set; }
        public decimal AllocationPercentage { get; set; }
    }
