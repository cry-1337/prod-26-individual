using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Queries.FeatureFlags;

public record GetFeatureFlagByIdQuery(Guid Id) : IRequest<FeatureFlagEntity>;