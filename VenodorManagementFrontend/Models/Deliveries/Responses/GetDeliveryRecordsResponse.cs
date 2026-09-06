using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VenodorManagementFrontend.Models;

public class GetDeliveryRecordsResponse
{
    [JsonPropertyName("deliveries")]
    public List<DeliveryRecordDto> Deliveries { get; set; } = new();

    [JsonPropertyName("deliveryRecords")]
    public List<DeliveryRecordDto> DeliveryRecords
    {
        get => Deliveries;
        set => Deliveries = value;
    }
}
