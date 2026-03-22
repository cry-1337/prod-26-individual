using System.Text.Json;

namespace LottyAB.Domain.Entities;

public class DecisionEntity : BaseEntity
{
    public string SubjectId { get; set; } = string.Empty;

    public Guid FeatureFlagId { get; set; }
    public FeatureFlagEntity FeatureFlag { get; set; } = null!;

    public Guid? ExperimentId { get; set; }
    public ExperimentEntity? Experiment { get; set; }

    public Guid? VariantId { get; set; }
    public VariantEntity? Variant { get; set; }

    public bool IsDefault { get; set; }

    public string VariantValue { get; set; } = string.Empty;

    public string? SubjectAttributesJson { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Dictionary<string, object>? GetSubjectAttributes()
    {
        if (string.IsNullOrWhiteSpace(SubjectAttributesJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(SubjectAttributesJson);
        }
        catch
        {
            return null;
        }
    }

    public void SetSubjectAttributes(Dictionary<string, object>? attributes)
    {
        if (attributes == null || attributes.Count == 0)
        {
            SubjectAttributesJson = null;
            return;
        }

        SubjectAttributesJson = JsonSerializer.Serialize(attributes);
    }
}