using LottyAB.Application.Commands.FeatureFlags;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.FeatureFlags;

public class DeactivateFeatureFlagHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeactivateFeatureFlagCommand, Unit>
{
    public async Task<Unit> Handle(DeactivateFeatureFlagCommand request, CancellationToken cancellationToken)
    {
        var featureFlag = await dbContext.FeatureFlags
            .FirstOrDefaultAsync(f => f.Id == request.Id, cancellationToken);

        if (featureFlag == null) throw new NotFoundException($"Feature flag with ID '{request.Id}' not found");

        featureFlag.IsActive = false;
        featureFlag.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}