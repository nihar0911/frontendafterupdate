using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class CreateOrganizationCommand
    {
        public string OrganizationName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
