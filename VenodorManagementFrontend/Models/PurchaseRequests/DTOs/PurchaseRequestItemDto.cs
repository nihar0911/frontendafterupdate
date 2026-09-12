using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VenodorManagementFrontend.Models;

public class PurchaseRequestItemDto
{
    [JsonPropertyName("requestItemID")]
    public int RequestItemID { get; set; }

    [JsonPropertyName("itemID")]
    public int ItemID
    {
        get => RequestItemID;
        set => RequestItemID = value;
    }

    public int RequestID { get; set; }
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int? VendorID { get; set; }
    public string? VendorName { get; set; }
}
