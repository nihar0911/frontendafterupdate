using System;
using System.Text.Json.Serialization;

namespace VendorManagement.Web.Models;

public class GetSpoilageAdviceResponse
{
    [JsonPropertyName("advisor")]
    public SpoilageAdvisorDto? Advisor { get; set; }
}
