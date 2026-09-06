using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetVendorProductsResponse
    {
        public List<VendorProductDto> VendorProducts { get; set; } = new();
    }
