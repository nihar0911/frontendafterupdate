using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetOutletsResponse
    {
        public List<OutletDto> Outlets { get; set; } = new();
    }
