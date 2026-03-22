using System.Text.Json.Serialization;
using LottyAB.Domain.Enums;

namespace LottyAB.Domain.Entities;

public class ExperimentReviewEntity : BaseEntity
{
    public Guid ExperimentId { get; set; }
    [JsonIgnore]
    public ExperimentEntity Experiment { get; set; } = null!;

    public Guid ReviewerId { get; set; }
    public UserEntity Reviewer { get; set; } = null!;

    public EReviewDecision Decision { get; set; }
    public string? Comment { get; set; }
}