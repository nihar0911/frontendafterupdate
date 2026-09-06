using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class QuotationDto
    {
        public int QuotationID { get; set; }
        public int RequestID { get; set; }
        public int VendorID { get; set; }
        public DateTime ValidUntil { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<QuotationItemDto> Items { get; set; } = new();
    }
