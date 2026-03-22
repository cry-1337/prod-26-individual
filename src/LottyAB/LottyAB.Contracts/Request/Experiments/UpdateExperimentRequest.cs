using LottyAB.Domain.Enums;

namespace LottyAB.Contracts.Request.Experiments;

public record UpdateExperimentRequest(
    string? Name = null,
    string? Description = null,
    double? AudienceFraction = null,
    string? TargetingRule = null,
    string? PrimaryMetricKey = null,
    List<VariantRequest>? Variants = null,
    Guid? ApproverGroupId = null,
    string? ConflictDomains = null,
    EConflictPolicy? ConflictPolicy = null,
    int? Priority = null);