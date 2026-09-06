using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class GetUsersResponse
    {
        public List<UserDto> Users { get; set; } = new();
    }
