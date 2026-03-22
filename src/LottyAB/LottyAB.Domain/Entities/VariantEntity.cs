using System.Text.Json.Serialization;

namespace LottyAB.Domain.Entities;

public class VariantEntity : BaseEntity
{
    public Guid ExperimentId { get; set; }
    [JsonIgnore]
    public ExperimentEntity Experiment { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public double Weight { get; set; }
    public bool IsControl { get; set; }

    public string? Description { get; set; }
}