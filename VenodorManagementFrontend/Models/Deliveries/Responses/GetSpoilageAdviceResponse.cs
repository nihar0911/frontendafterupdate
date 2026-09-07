using System;
using System.Text.Json.Serialization;

namespace VenodorManagementFrontend.Models;

public class GetSpoilageAdviceResponse
{
    [JsonPropertyName("advisor")]
    public SpoilageAdvisorDto? Advisor { get; set; }
}
