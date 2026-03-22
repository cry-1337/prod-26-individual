using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.FeatureFlags;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.FeatureFlags;

public class GetFeatureFlagsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetFeatureFlagsQuery, PagedResponse<FeatureFlagEntity>>
{
    public async Task<PagedResponse<FeatureFlagEntity>> Handle(GetFeatureFlagsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.FeatureFlags.AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(f => f.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var featureFlags = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<FeatureFlagEntity>
        {
            Items = featureFlags,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}