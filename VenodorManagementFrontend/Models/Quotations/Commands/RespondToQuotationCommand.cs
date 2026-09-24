using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class RespondToQuotationCommand
    {
        public int QuotationID { get; set; }
        public string Status { get; set; } = "Accepted"; // "Accepted" or "Rejected"
    }
