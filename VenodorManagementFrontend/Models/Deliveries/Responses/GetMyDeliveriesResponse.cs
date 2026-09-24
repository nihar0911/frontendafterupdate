using System.Collections.Generic;
using VendorManagement.Web.Models.Deliveries.DTOs;

namespace VendorManagement.Web.Models.Deliveries.Responses;

public class GetMyDeliveriesResponse
{
    public List<VendorDeliveryItemDto> Deliveries { get; set; } = new();
    public int TotalDeliveries { get; set; }
    public decimal TotalReceivedQuantity { get; set; }
    public decimal TotalSpoiledQuantity { get; set; }
    public int DistinctProductsCount { get; set; }
}
