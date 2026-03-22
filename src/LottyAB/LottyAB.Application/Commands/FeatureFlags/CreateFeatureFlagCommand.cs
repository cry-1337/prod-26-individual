using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;

namespace LottyAB.Application.Commands.FeatureFlags;

public record CreateFeatureFlagCommand(
    string Key,
    string Name,
    string? Description,
    EFeatureFlagType ValueType,
    string DefaultValue) : IRequest<FeatureFlagEntity>;