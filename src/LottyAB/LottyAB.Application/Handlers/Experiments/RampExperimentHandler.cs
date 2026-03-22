using System.Text.Json;
using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace LottyAB.Application.Handlers.Experiments;

public class RampExperimentHandler(
    IApplicationDbContext dbContext,
    IDistributedCache cache,
    INotificationService notificationService)
    : IRequestHandler<RampExperimentCommand, ExperimentEntity>
{
    public async Task<ExperimentEntity> Handle(RampExperimentCommand request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .Include(e => e.Variants)
            .Include(e => e.FeatureFlag)
            .FirstOrDefaultAsync(e => e.Id == request.ExperimentId, cancellationToken);

        if (experiment == null)
            throw new NotFoundException($"Experiment with ID '{request.ExperimentId}' not found");

        if (experiment.Status != EExperimentStatus.Running)
            throw new UnprocessableEntityException($"Cannot ramp experiment in '{experiment.Status}' status. Only Running experiments can be ramped.");

        if (request.NewAudienceFraction <= experiment.AudienceFraction)
            throw new UnprocessableEntityException($"New audience fraction ({request.NewAudienceFraction}) must be greater than current ({experiment.AudienceFraction}).");

        if (request.NewAudienceFraction > 1.0)
            throw new UnprocessableEntityException("Audience fraction cannot exceed 1.0.");

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
            ChangeReason = "Audience fraction ramped up"
        };

        dbContext.ExperimentVersions.Add(version);

        var oldFraction = experiment.AudienceFraction;
        var newFraction = request.NewAudienceFraction;

        foreach (var variant in experiment.Variants)
            variant.Weight = variant.Weight / oldFraction * newFraction;

        experiment.AudienceFraction = newFraction;
        experiment.Version++;
        experiment.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await cache.RemoveAsync($"flag:{experiment.FeatureFlag.Key}", cancellationToken);
        await notificationService.NotifyAsync(
            $"📈 Раскатка: {experiment.Name} | {oldFraction * 100:F0}% → {newFraction * 100:F0}%",
            cancellationToken);

        return experiment;
    }
}