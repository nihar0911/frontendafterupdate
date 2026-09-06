using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class CreatePurchaseRequestCommand
    {
        public int OutletID { get; set; }
        public int CreatedByUserID { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public List<int>? SelectedVendorIDs { get; set; }
        public int? SelectedVendorID { get; set; }
        public List<CreatePurchaseRequestItemDto> Items { get; set; } = new();
    }
