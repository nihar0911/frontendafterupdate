using System;
using System.Collections.Generic;

namespace VendorManagement.Web.Models;

public class GetContractEligibleVendorsResponse
{
    public int OutletID { get; set; }
    public int ProductID { get; set; }
    public bool HasActiveContracts { get; set; }
    public List<VendorRecommendationDto> Vendors { get; set; } = new();
}
