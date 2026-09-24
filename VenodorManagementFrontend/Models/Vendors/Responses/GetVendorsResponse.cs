using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetVendorsResponse
    {
        public List<VendorDto> Vendors { get; set; } = new();
    }
