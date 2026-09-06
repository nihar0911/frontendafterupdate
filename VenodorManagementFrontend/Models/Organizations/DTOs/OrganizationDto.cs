using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class OrganizationDto
    {
        public int OrganizationID { get; set; }
        public string OrganizationName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public DateTime CreatedAt { get; set; }
    }
