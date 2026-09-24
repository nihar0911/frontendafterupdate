using System;

namespace VendorManagement.Web.Models;

public class ContractProductDto
{
    public int ContractProductID { get; set; }
    public int ContractID { get; set; }
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal ContractQuantity { get; set; }
    public decimal PurchasedQuantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal VarianceQuantity => PurchasedQuantity - ContractQuantity;
    public decimal RemainingQuantity => Math.Max(ContractQuantity - PurchasedQuantity, 0m);
    public decimal ExtraOrderQuantity => Math.Max(PurchasedQuantity - ContractQuantity, 0m);
}
