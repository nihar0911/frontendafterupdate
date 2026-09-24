using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetUsersResponse
    {
        public List<UserDto> Users { get; set; } = new();
    }
