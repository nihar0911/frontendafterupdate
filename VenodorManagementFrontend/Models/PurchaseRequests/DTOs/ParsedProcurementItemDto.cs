using System.Collections.Generic;

namespace VenodorManagementFrontend.Models;

public class ParsedProcurementItemDto
{
    public int? ProductID { get; set; }
    public string? ProductName { get; set; }
    public string SpokenProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string ResolutionStatus { get; set; } = string.Empty; // Resolved, NotFound, Ambiguous, InvalidQuantity
    public string? Message { get; set; }
    public List<string>? AmbiguousMatches { get; set; }
}
