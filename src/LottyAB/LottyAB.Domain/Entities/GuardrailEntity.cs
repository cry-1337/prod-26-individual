using LottyAB.Domain.Enums;

namespace LottyAB.Domain.Entities;

public class GuardrailEntity : BaseEntity
{
    public Guid ExperimentId { get; set; }
    public ExperimentEntity Experiment { get; set; } = null!;

    public string MetricKey { get; set; } = string.Empty;

    public double Threshold { get; set; }

    public int ObservationWindowMinutes { get; set; }

    public EGuardrailAction Action { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<GuardrailTriggerHistoryEntity> TriggerHistory { get; set; } = new List<GuardrailTriggerHistoryEntity>();
}