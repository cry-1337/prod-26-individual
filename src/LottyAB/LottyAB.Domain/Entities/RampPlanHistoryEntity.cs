using LottyAB.Domain.Enums;
using System.Text.Json.Serialization;

namespace LottyAB.Domain.Entities;

public class RampPlanHistoryEntity : BaseEntity
{
    public Guid RampPlanId { get; set; }
    [JsonIgnore]
    public RampPlanEntity RampPlan { get; set; } = null!;

    public Guid ExperimentId { get; set; }

    public ERampPlanAction Action { get; set; }

    public double FromFraction { get; set; }
    public double ToFraction { get; set; }

    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}