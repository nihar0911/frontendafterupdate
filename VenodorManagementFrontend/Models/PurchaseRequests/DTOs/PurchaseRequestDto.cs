using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class PurchaseRequestDto
    {
        public int RequestID { get; set; }
        public int OutletID { get; set; }
        public string OutletName { get; set; } = string.Empty;
        public int CreatedByUserID { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<PurchaseRequestItemDto> Items { get; set; } = new();
    }
