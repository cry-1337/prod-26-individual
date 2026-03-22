using LottyAB.Domain.Enums;

namespace LottyAB.Contracts.Request.FeatureFlags;

public record CreateFeatureFlagRequest(
    string Key,
    string Name,
    string? Description,
    EFeatureFlagType ValueType,
    string DefaultValue);