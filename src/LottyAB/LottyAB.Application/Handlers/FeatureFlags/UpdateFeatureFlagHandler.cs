using LottyAB.Application.Commands.FeatureFlags;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.FeatureFlags;

public class UpdateFeatureFlagHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateFeatureFlagCommand, FeatureFlagEntity>
{
    public async Task<FeatureFlagEntity> Handle(UpdateFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        var featureFlag = await dbContext.FeatureFlags
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        if (featureFlag == null) throw new NotFoundException($"Feature flag with ID '{request.Id}' not found");

        if (request.Name != null)
            featureFlag.Name = request.Name;

        if (request.Description != null)
            featureFlag.Description = request.Description;

        if (request.DefaultValue != null)
            featureFlag.DefaultValue = request.DefaultValue;

        if (request.IsActive.HasValue)
            featureFlag.IsActive = request.IsActive.Value;

        featureFlag.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return featureFlag;
    }
}