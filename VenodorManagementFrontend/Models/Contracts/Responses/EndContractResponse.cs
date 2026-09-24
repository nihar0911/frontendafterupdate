using System;

namespace VendorManagement.Web.Models;

public class EndContractResponse
{
    public ContractDto? Contract { get; set; }
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
}
