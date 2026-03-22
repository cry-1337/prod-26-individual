using LottyAB.Domain.Enums;

namespace LottyAB.Contracts.Request.Experiments;

public record CreateExperimentRequest(
    string Name,
    string? Description,
    Guid FeatureFlagId,
    double AudienceFraction,
    string? TargetingRule,
    string? PrimaryMetricKey,
    List<VariantRequest> Variants,
    Guid? ApproverGroupId = null,
    string? ConflictDomains = null,
    EConflictPolicy? ConflictPolicy = null,
    int Priority = 0);