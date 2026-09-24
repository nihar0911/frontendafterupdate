using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetOrganizationsResponse
    {
        public List<OrganizationDto> Organizations { get; set; } = new();
    }
