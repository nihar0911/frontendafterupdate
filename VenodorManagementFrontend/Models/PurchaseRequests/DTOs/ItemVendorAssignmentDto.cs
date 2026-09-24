using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class ItemVendorAssignmentDto
{
    public int RequestItemID { get; set; }
    public int ProductID { get; set; }
    public int VendorID { get; set; }
}
