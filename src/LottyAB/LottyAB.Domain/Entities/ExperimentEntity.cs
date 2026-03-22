using LottyAB.Domain.Enums;

namespace LottyAB.Domain.Entities;

public class ExperimentEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid FeatureFlagId { get; set; }
    public FeatureFlagEntity FeatureFlag { get; set; } = null!;

    public EExperimentStatus Status { get; set; } = EExperimentStatus.Draft;

    public int Version { get; set; } = 1;

    public double AudienceFraction { get; set; }
    public string? TargetingRule { get; set; }

    public Guid OwnerId { get; set; }
    public UserEntity Owner { get; set; } = null!;

    public Guid? ApproverGroupId { get; set; }
    public ApproverGroupEntity? ApproverGroup { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public ECompletionOutcome? Outcome { get; set; }
    public string? OutcomeComment { get; set; }

    public string? PrimaryMetricKey { get; set; }
    public string? GuardrailMetricKeys { get; set; }

    public string? ConflictDomains { get; set; }
    public EConflictPolicy? ConflictPolicy { get; set; }
    public int Priority { get; set; }

    public ICollection<VariantEntity> Variants { get; set; } = new List<VariantEntity>();
    public ICollection<ExperimentReviewEntity> Reviews { get; set; } = new List<ExperimentReviewEntity>();
    public ICollection<ExperimentVersionEntity> Versions { get; set; } = new List<ExperimentVersionEntity>();
    public ICollection<GuardrailEntity> Guardrails { get; set; } = new List<GuardrailEntity>();
    public ICollection<GuardrailTriggerHistoryEntity> GuardrailTriggers { get; set; } = new List<GuardrailTriggerHistoryEntity>();
    public RampPlanEntity? RampPlan { get; set; }
}