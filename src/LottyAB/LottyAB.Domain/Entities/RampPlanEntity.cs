using LottyAB.Domain.Enums;
using System.Text.Json.Serialization;

namespace LottyAB.Domain.Entities;

public class RampPlanEntity : BaseEntity
{
    public Guid ExperimentId { get; set; }
    [JsonIgnore]
    public ExperimentEntity Experiment { get; set; } = null!;

    public string StepsJson { get; set; } = "[0.05,0.25,0.50,1.0]";
    public int CurrentStepIndex { get; set; } = 0;
    public int MinImpressionsPerStep { get; set; } = 100;
    public int MinMinutesPerStep { get; set; } = 60;
    public ERampSafetyAction SafetyAction { get; set; }

    public bool IsEnabled { get; set; } = true;
    public bool IsCompleted { get; set; } = false;
    public DateTime StepEnteredAt { get; set; } = DateTime.UtcNow;

    public ICollection<RampPlanHistoryEntity> History { get; set; } = new List<RampPlanHistoryEntity>();
}