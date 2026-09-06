using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetOrganizationsResponse
    {
        public List<OrganizationDto> Organizations { get; set; } = new();
    }
