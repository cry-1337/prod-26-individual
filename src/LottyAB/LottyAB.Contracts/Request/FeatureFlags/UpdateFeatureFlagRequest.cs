namespace LottyAB.Contracts.Request.FeatureFlags;

public record UpdateFeatureFlagRequest(
    string? Name = null,
    string? Description = null,
    string? DefaultValue = null,
    bool? IsActive = null);