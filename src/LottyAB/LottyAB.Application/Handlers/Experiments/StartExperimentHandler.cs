using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Experiments;

public class StartExperimentHandler(IApplicationDbContext dbContext, INotificationService notificationService)
    : IRequestHandler<StartExperimentCommand, ExperimentEntity>
{
    public async Task<ExperimentEntity> Handle(StartExperimentCommand request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .Include(e => e.FeatureFlag)
            .FirstOrDefaultAsync(e => e.Id == request.ExperimentId, cancellationToken);

        if (experiment == null)
            throw new NotFoundException($"Experiment with ID '{request.ExperimentId}' not found");

        if (experiment.Status != EExperimentStatus.Approved)
            throw new UnprocessableEntityException($"Cannot start experiment in '{experiment.Status}' status. Only Approved experiments can be started.");

        var activeExperiment = await dbContext.Experiments
            .FirstOrDefaultAsync(e => e.FeatureFlagId == experiment.FeatureFlagId &&
                                    e.Id != experiment.Id &&
                                    (e.Status == EExperimentStatus.Running || e.Status == EExperimentStatus.Paused),
                                    cancellationToken);

        if (activeExperiment != null)
            throw new UnprocessableEntityException($"Feature flag '{experiment.FeatureFlag.Key}' already has an active experiment");

        experiment.Status = EExperimentStatus.Running;
        experiment.StartedAt = DateTime.UtcNow;
        experiment.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync($"🚀 Эксперимент запущен: {experiment.Name}", cancellationToken);

        return experiment;
    }
}