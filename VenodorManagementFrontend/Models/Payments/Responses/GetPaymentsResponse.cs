using System.Collections.Generic;
using VenodorManagementFrontend.Models.Payments.DTOs;

namespace VenodorManagementFrontend.Models.Payments.Responses;

public class GetPaymentsResponse
{
    public List<PaymentDto> Payments { get; set; } = new();
}
