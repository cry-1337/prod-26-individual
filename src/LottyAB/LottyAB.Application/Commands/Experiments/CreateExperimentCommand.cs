using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;

namespace LottyAB.Application.Commands.Experiments;

public record CreateExperimentCommand(
    string Name,
    string? Description,
    Guid FeatureFlagId,
    double AudienceFraction,
    string? TargetingRule,
    string? PrimaryMetricKey,
    Guid OwnerId,
    List<VariantCommand> Variants,
    Guid? ApproverGroupId = null,
    string? ConflictDomains = null,
    EConflictPolicy? ConflictPolicy = null,
    int Priority = 0) : IRequest<ExperimentEntity>;

public record VariantCommand(
    string Name,
    string Value,
    double Weight,
    bool IsControl);