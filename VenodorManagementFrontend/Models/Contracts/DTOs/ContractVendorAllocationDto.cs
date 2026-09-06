using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class ContractVendorAllocationDto
    {
        public int AllocationID { get; set; }
        public int ContractID { get; set; }
        public int VendorID { get; set; }
        public decimal AllocationPercentage { get; set; }
        public decimal AllocatedQuantity { get; set; }
        public decimal UsedQuantity { get; set; }
        public decimal RemainingQuantity => AllocatedQuantity - UsedQuantity;
        public string VendorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
