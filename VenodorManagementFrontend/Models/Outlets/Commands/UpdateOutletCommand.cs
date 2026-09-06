using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class UpdateOutletCommand
    {
        public int OutletID { get; set; }
        public int OrganizationID { get; set; }
        public string OutletName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string PurchaseOrderApproverRole { get; set; } = "Organization Manager";
    }
