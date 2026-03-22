using LottyAB.Domain.Enums;

namespace LottyAB.Contracts.Responses;

public class GuardrailTriggerHistoryResponse
{
    public Guid Id { get; set; }
    public Guid GuardrailId { get; set; }
    public Guid ExperimentId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public double Threshold { get; set; }
    public double ActualValue { get; set; }
    public EGuardrailAction ActionTaken { get; set; }
    public DateTime TriggeredAt { get; set; }
    public int ObservationWindowMinutes { get; set; }
}