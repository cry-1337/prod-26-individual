using LottyAB.Application.Commands.FeatureFlags;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.FeatureFlags;

public class CreateFeatureFlagHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateFeatureFlagCommand, FeatureFlagEntity>
{
    public async Task<FeatureFlagEntity> Handle(CreateFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        var existingFlag = await dbContext.FeatureFlags
            .FirstOrDefaultAsync(f => f.Key == request.Key, cancellationToken);

        if (existingFlag != null) throw new ConflictException($"Feature flag with key '{request.Key}' already exists");

        var featureFlag = new FeatureFlagEntity
        {
            Key = request.Key,
            Name = request.Name,
            Description = request.Description,
            ValueType = request.ValueType,
            DefaultValue = request.DefaultValue,
            IsActive = true
        };

        await dbContext.FeatureFlags.AddAsync(featureFlag, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return featureFlag;
    }
}