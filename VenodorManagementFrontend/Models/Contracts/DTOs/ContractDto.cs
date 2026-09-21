using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VenodorManagementFrontend.Models;

public class ContractDto
{
    public int ContractID { get; set; }
    public int OutletID { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public int OrganizationID { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int? RequestID { get; set; }
    public int? QuotationID { get; set; }
    public int? VendorID { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal UsedQuantity { get; set; }
    public decimal RemainingQuantity => Math.Max(TotalQuantity - UsedQuantity, 0m);
    public decimal ExtraOrderQuantity => Math.Max(UsedQuantity - TotalQuantity, 0m);

    public decimal? ContractQuantity { get; set; }
    public decimal? PurchasedQuantity { get; set; }
    public decimal? ContractTotalQuantity { get; set; }
    public decimal? VarianceQuantity => PurchasedQuantity.HasValue && ContractQuantity.HasValue
        ? PurchasedQuantity.Value - ContractQuantity.Value
        : null;

    public decimal UnitPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;

    private string _status = string.Empty;
    public string Status
    {
        get
        {
            // Backend API response is single source of truth; dynamic fallback only for boundary/in-memory
            if (string.Equals(_status, "Active", StringComparison.OrdinalIgnoreCase) && DateTime.Now > EndDate)
            {
                return "Expired";
            }
            return _status;
        }
        set => _status = value;
    }

    public List<ContractVendorAllocationDto> Allocations { get; set; } = new();

    private List<ContractProductDto> _products = new();

    [JsonPropertyName("products")]
    public List<ContractProductDto> Products
    {
        get => _products;
        set => _products = value ?? new();
    }

    [JsonPropertyName("contractProducts")]
    public List<ContractProductDto> ContractProducts
    {
        get => _products;
        set => _products = value ?? new();
    }
}
