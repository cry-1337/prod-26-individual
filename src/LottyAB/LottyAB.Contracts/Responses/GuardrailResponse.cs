using System.Text.Json.Serialization;
using LottyAB.Domain.Enums;

namespace LottyAB.Contracts.Responses;

public class GuardrailResponse
{
    public Guid Id { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public double Threshold { get; set; }
    public int ObservationWindowMinutes { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EGuardrailAction Action { get; set; }
    public bool IsActive { get; set; }
}