using System;

namespace VenodorManagementFrontend.Models;

public class ContractProductAssignmentDto
{
    public int ProductID { get; set; }
    public int VendorID { get; set; }
    public decimal ContractQuantity { get; set; }
}
