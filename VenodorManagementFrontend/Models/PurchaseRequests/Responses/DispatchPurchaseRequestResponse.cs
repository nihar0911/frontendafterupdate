using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class DispatchPurchaseRequestResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public int OpportunitiesCreated { get; set; }
        public int NotificationsSent { get; set; }
    }
