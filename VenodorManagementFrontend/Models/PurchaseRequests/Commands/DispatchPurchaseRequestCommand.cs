using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class DispatchPurchaseRequestCommand
    {
        public int RequestID { get; set; }
        public List<int> SelectedVendorIDs { get; set; } = new();
    }
