using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class RespondToOpportunityCommand
    {
        public int RequestID { get; set; }
        public int ProductID { get; set; }
        public string Action { get; set; } = string.Empty; // "Accept" or "Reject"
        public string? RejectionReason { get; set; }
    }
