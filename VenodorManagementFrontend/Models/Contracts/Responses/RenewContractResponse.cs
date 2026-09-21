namespace VenodorManagementFrontend.Models;

public class RenewContractResponse
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public ContractDto? Contract { get; set; }
}
