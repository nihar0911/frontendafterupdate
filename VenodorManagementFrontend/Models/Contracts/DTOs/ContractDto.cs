using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class ContractDto
    {
        public int ContractID { get; set; }
        public int OutletID { get; set; }
        public string OutletName { get; set; } = string.Empty;
        public int OrganizationID { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int? RequestID { get; set; }
        public int? QuotationID { get; set; }
        public int? VendorID { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public decimal TotalQuantity { get; set; }
        public decimal UsedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<ContractVendorAllocationDto> Allocations { get; set; } = new();
    }
