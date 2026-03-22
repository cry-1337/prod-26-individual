using System.Text.Json.Serialization;
using LottyAB.Domain.Enums;

namespace LottyAB.Domain.Entities;

public class ExperimentVersionEntity : BaseEntity
{
    public Guid ExperimentId { get; set; }
    [JsonIgnore]
    public ExperimentEntity Experiment { get; set; } = null!;

    public int Version { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public double AudienceFraction { get; set; }
    public string? TargetingRule { get; set; }

    public string? PrimaryMetricKey { get; set; }
    public string? GuardrailMetricKeys { get; set; }

    public EExperimentStatus Status { get; set; }

    public string VariantsSnapshot { get; set; } = string.Empty;

    public Guid ChangedBy { get; set; }
    public string ChangeReason { get; set; } = string.Empty;
}