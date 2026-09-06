using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class VendorProcurementOpportunityDto
    {
        public int RequestID { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int OrganizationID { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public int OutletID { get; set; }
        public string OutletName { get; set; } = string.Empty;
        public string? OutletAddress { get; set; }
        public int CreatedByUserID { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public string CreatedByUserRole { get; set; } = string.Empty;
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
        public int VendorID { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int EstimatedDeliveryDays { get; set; }
        public string OpportunityStatus { get; set; } = "Pending"; // Pending, Accepted, Rejected
        public string? RejectionReason { get; set; }
        public DateTime? ResponseDate { get; set; }
        public bool HasQuotation { get; set; }
        public int? QuotationID { get; set; }
        public string? QuotationStatus { get; set; }
    }
