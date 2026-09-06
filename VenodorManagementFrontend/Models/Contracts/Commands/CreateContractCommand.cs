using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class CreateContractCommand
    {
        public int? QuotationID { get; set; }
        public int OutletID { get; set; }
        public int ProductID { get; set; }
        public decimal TotalQuantity { get; set; }
        public DateTime StartDate { get; set; } = DateTime.Now.AddDays(-1);
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(365);
        public string PaymentMethod { get; set; } = "Net30";
        public List<CreateContractVendorAllocationDto> Allocations { get; set; } = new();
    }
