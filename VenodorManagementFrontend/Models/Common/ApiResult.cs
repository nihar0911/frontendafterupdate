using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class ApiResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
