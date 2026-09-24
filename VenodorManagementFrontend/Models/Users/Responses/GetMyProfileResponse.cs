using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetMyProfileResponse
{
    public LoginResponse? Profile { get; set; }
    public UserDto? User { get; set; }
    public int UserID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
    public int? OrganizationID { get; set; }
    public int? OutletID { get; set; }
    public int? VendorID { get; set; }
}
