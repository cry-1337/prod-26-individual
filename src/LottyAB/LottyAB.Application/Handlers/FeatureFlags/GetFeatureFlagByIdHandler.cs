using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.FeatureFlags;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.FeatureFlags;

public class GetFeatureFlagByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetFeatureFlagByIdQuery, FeatureFlagEntity>
{
    public async Task<FeatureFlagEntity> Handle(GetFeatureFlagByIdQuery request, CancellationToken cancellationToken)
    {
        var featureFlag = await dbContext.FeatureFlags
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        return featureFlag ?? throw new NotFoundException($"Feature flag with ID '{request.Id}' not found");
    }
}