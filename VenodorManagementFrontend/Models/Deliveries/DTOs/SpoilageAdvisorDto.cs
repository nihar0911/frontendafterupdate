using System;
using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class SpoilageAdvisorDto
{
    public int PurchaseOrderID { get; set; }
    public int VendorID { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public bool IsSingleProduct { get; set; }
    public string OverallSummary { get; set; } = string.Empty;
    public List<ProductSpoilageAdviceDto> Products { get; set; } = new();
}
