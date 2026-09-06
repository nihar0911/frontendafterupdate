using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class VendorDto
    {
        public int VendorID { get; set; }
        public string VendorName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? GSTIN { get; set; }
        public string Status { get; set; } = string.Empty;
    }
