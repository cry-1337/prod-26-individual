using LottyAB.Domain.Enums;

namespace LottyAB.Domain.Entities;

public class GuardrailTriggerHistoryEntity : BaseEntity
{
    public Guid GuardrailId { get; set; }
    public GuardrailEntity Guardrail { get; set; } = null!;

    public Guid ExperimentId { get; set; }
    public ExperimentEntity Experiment { get; set; } = null!;

    public string MetricKey { get; set; } = string.Empty;

    public double Threshold { get; set; }

    public double ActualValue { get; set; }

    public EGuardrailAction ActionTaken { get; set; }

    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    public int ObservationWindowMinutes { get; set; }
}