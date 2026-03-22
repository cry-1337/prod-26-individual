using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Application.Targeting;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Experiments;

public class CreateExperimentHandler(IApplicationDbContext dbContext, ITargetingParser parser)
    : IRequestHandler<CreateExperimentCommand, ExperimentEntity>
{
    public async Task<ExperimentEntity> Handle(CreateExperimentCommand request, CancellationToken cancellationToken)
    {
        var featureFlag = await dbContext.FeatureFlags
            .FirstOrDefaultAsync(f => f.Id == request.FeatureFlagId, cancellationToken);

        if (featureFlag == null)
            throw new NotFoundException($"Feature flag with ID '{request.FeatureFlagId}' not found");

        var activeExperiment = await dbContext.Experiments
            .FirstOrDefaultAsync(e => e.FeatureFlagId == request.FeatureFlagId &&
                                    (e.Status == EExperimentStatus.Running || e.Status == EExperimentStatus.Paused),
                                    cancellationToken);

        if (activeExperiment != null)
            throw new ConflictException($"Feature flag '{featureFlag.Key}' already has an active experiment");

        var controlVariantsCount = request.Variants.Count(v => v.IsControl);
        if (controlVariantsCount != 1)
            throw new UnprocessableEntityException("Experiment must have exactly one control variant");

        var totalWeight = request.Variants.Sum(v => v.Weight);
        if (Math.Abs(totalWeight - request.AudienceFraction) > 0.001)
            throw new UnprocessableEntityException($"Sum of variant weights ({totalWeight}) must equal audience fraction ({request.AudienceFraction})");

        if (request.TargetingRule != null)
            parser.Parse(request.TargetingRule);

        var experiment = new ExperimentEntity
        {
            Name = request.Name,
            Description = request.Description,
            FeatureFlagId = request.FeatureFlagId,
            AudienceFraction = request.AudienceFraction,
            TargetingRule = request.TargetingRule,
            PrimaryMetricKey = request.PrimaryMetricKey,
            OwnerId = request.OwnerId,
            ApproverGroupId = request.ApproverGroupId,
            Status = EExperimentStatus.Draft,
            Version = 1,
            ConflictDomains = request.ConflictDomains,
            ConflictPolicy = request.ConflictPolicy,
            Priority = request.Priority
        };

        foreach (var variantRequest in request.Variants)
        {
            experiment.Variants.Add(new VariantEntity
            {
                Name = variantRequest.Name,
                Value = variantRequest.Value,
                Weight = variantRequest.Weight,
                IsControl = variantRequest.IsControl
            });
        }

        dbContext.Experiments.Add(experiment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return experiment;
    }
}