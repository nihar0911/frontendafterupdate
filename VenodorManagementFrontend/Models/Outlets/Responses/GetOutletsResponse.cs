using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetOutletsResponse
    {
        public List<OutletDto> Outlets { get; set; } = new();
    }
