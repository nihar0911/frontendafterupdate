using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class ApiResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
    }
