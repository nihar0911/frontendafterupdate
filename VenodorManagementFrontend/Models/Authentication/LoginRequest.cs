using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
