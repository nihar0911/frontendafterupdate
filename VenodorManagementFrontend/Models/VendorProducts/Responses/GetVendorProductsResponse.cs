using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetVendorProductsResponse
    {
        public List<VendorProductDto> VendorProducts { get; set; } = new();
    }
