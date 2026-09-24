using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetProductsResponse
    {
        public List<ProductDto> Products { get; set; } = new();
    }
