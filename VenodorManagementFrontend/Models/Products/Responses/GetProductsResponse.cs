using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetProductsResponse
    {
        public List<ProductDto> Products { get; set; } = new();
    }
