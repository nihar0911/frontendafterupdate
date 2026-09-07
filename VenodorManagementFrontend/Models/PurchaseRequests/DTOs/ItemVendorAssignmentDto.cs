using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class ItemVendorAssignmentDto
{
    public int RequestItemID { get; set; }
    public int ProductID { get; set; }
    public int VendorID { get; set; }
}
