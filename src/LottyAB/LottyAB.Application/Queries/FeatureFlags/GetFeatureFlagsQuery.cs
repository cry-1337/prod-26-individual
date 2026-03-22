using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Queries.FeatureFlags;

public record GetFeatureFlagsQuery(
    int PageNumber,
    int PageSize,
    bool? IsActive) : IRequest<PagedResponse<FeatureFlagEntity>>;