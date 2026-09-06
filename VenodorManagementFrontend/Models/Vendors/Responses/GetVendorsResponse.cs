using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetVendorsResponse
    {
        public List<VendorDto> Vendors { get; set; } = new();
    }
