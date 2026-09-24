using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class OutletDto
{
    public int OutletID { get; set; }
    public int OrganizationID { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string OutletName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string PurchaseOrderApproverRole { get; set; } = "Organization Manager";
    public string Status { get; set; } = "Active";
}
