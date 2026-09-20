using System;

namespace VenodorManagementFrontend.Models;

public class ContractProductDto
{
    public int ContractProductID { get; set; }
    public int ContractID { get; set; }
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal ContractQuantity { get; set; }
    public decimal PurchasedQuantity { get; set; }
}
