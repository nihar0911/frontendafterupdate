using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class RenewContractProductItemDto
{
    public int ProductID { get; set; }
    public decimal ContractQuantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class RenewContractCommand
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public List<RenewContractProductItemDto> Products { get; set; } = new();
}
