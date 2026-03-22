using MediatR;

namespace LottyAB.Application.Commands.FeatureFlags;

public record DeactivateFeatureFlagCommand(Guid Id) : IRequest<Unit>;