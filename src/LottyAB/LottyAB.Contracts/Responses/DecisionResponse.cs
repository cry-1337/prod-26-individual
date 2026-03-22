using LottyAB.Domain.Enums;

namespace LottyAB.Contracts.Responses;

public class DecisionResponse
{
    public Guid DecisionId { get; set; }
    public string VariantKey { get; set; } = string.Empty;
    public string VariantValue { get; set; } = string.Empty;
    public object? TypedValue { get; set; }
    public EFeatureFlagType ValueType { get; set; }
    public bool IsDefault { get; set; }
    public string FeatureFlagKey { get; set; } = string.Empty;
    public Guid? ExperimentId { get; set; }
    public Guid? VariantId { get; set; }
}