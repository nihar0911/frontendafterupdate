using System.Collections.Generic;
using VendorManagement.Web.Models.Payments.DTOs;

namespace VendorManagement.Web.Models.Payments.Responses;

public class GetPaymentsResponse
{
    public List<PaymentDto> Payments { get; set; } = new();
}
