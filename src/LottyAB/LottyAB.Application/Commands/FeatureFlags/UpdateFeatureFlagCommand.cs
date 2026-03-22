using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Commands.FeatureFlags;

public record UpdateFeatureFlagCommand(
    Guid Id,
    string? Name,
    string? Description,
    string? DefaultValue,
    bool? IsActive) : IRequest<FeatureFlagEntity>;