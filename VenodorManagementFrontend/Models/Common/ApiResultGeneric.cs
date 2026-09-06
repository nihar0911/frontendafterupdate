using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class ApiResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
    }
