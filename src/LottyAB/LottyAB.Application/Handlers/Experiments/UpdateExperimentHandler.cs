using System.Text.Json;
using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Experiments;

public class UpdateExperimentHandler(IApplicationDbContext dbContext)
    : IRequestHandler<UpdateExperimentCommand, ExperimentEntity>
{
    public async Task<ExperimentEntity> Handle(UpdateExperimentCommand request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .Include(e => e.Variants)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (experiment == null)
            throw new NotFoundException($"Experiment with ID '{request.Id}' not found");

        if (experiment.Status != EExperimentStatus.Draft)
            throw new UnprocessableEntityException($"Cannot update experiment in '{experiment.Status}' status. Only Draft experiments can be updated.");

        var variantsSnapshot = experiment.Variants.Select(v => new
        {
            v.Name,
            v.Value,
            v.Weight,
            v.IsControl
        }).ToList();

        var version = new ExperimentVersionEntity
        {
            ExperimentId = experiment.Id,
            Version = experiment.Version,
            Name = experiment.Name,
            Description = experiment.Description,
            AudienceFraction = experiment.AudienceFraction,
            TargetingRule = experiment.TargetingRule,
            PrimaryMetricKey = experiment.PrimaryMetricKey,
            GuardrailMetricKeys = experiment.GuardrailMetricKeys,
            Status = experiment.Status,
            VariantsSnapshot = JsonSerializer.Serialize(variantsSnapshot),
            ChangedBy = request.UserId,
            ChangeReason = "Experiment configuration updated"
        };

        dbContext.ExperimentVersions.Add(version);

        experiment.Version++;

        if (request.Name != null)
            experiment.Name = request.Name;

        if (request.Description != null)
            experiment.Description = request.Description;

        if (request.AudienceFraction.HasValue)
            experiment.AudienceFraction = request.AudienceFraction.Value;

        if (request.TargetingRule != null)
            experiment.TargetingRule = request.TargetingRule;

        if (request.PrimaryMetricKey != null)
            experiment.PrimaryMetricKey = request.PrimaryMetricKey;

        if (request.Variants != null)
        {
            var controlVariantsCount = request.Variants.Count(v => v.IsControl);
            if (controlVariantsCount != 1)
                throw new UnprocessableEntityException("Experiment must have exactly one control variant");

            var totalWeight = request.Variants.Sum(v => v.Weight);
            var audienceFraction = request.AudienceFraction ?? experiment.AudienceFraction;

            if (Math.Abs(totalWeight - audienceFraction) > 0.001)
                throw new UnprocessableEntityException($"Sum of variant weights ({totalWeight}) must equal audience fraction ({audienceFraction})");

            dbContext.Variants.RemoveRange(experiment.Variants);

            experiment.Variants.Clear();
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
        }

        if (request.ApproverGroupId.HasValue)
            experiment.ApproverGroupId = request.ApproverGroupId;

        if (request.ConflictDomains != null)
            experiment.ConflictDomains = request.ConflictDomains;

        if (request.ConflictPolicy.HasValue)
            experiment.ConflictPolicy = request.ConflictPolicy;

        if (request.Priority.HasValue)
            experiment.Priority = request.Priority.Value;

        experiment.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return experiment;
    }
}