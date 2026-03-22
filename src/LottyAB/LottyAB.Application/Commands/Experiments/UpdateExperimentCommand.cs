using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;

namespace LottyAB.Application.Commands.Experiments;

public record UpdateExperimentCommand(
    Guid Id,
    Guid UserId,
    string? Name,
    string? Description,
    double? AudienceFraction,
    string? TargetingRule,
    string? PrimaryMetricKey,
    List<VariantCommand>? Variants,
    Guid? ApproverGroupId = null,
    string? ConflictDomains = null,
    EConflictPolicy? ConflictPolicy = null,
    int? Priority = null) : IRequest<ExperimentEntity>;