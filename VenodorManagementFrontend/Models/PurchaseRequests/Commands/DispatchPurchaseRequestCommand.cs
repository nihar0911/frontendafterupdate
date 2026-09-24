using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class DispatchPurchaseRequestCommand
    {
        public int RequestID { get; set; }
        public List<int> SelectedVendorIDs { get; set; } = new();
        public List<ItemVendorAssignmentDto>? ItemVendorAssignments { get; set; } = new();
    }
