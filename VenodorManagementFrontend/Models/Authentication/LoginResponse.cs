using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public int UserID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? OrganizationID { get; set; }
        public int? OutletID { get; set; }
        public int? VendorID { get; set; }
    }
