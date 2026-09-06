using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class UpdateMyProfileCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? NewPassword { get; set; }
    }
