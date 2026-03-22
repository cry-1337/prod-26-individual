using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace LottyAB.Application.Handlers.Experiments;

public class CompleteExperimentHandler(
    IApplicationDbContext dbContext,
    IDistributedCache cache,
    INotificationService notificationService)
    : IRequestHandler<CompleteExperimentCommand, ExperimentEntity>
{
    public async Task<ExperimentEntity> Handle(CompleteExperimentCommand request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .FirstOrDefaultAsync(e => e.Id == request.ExperimentId, cancellationToken);

        if (experiment == null)
            throw new NotFoundException($"Experiment with ID '{request.ExperimentId}' not found");

        if (experiment.Status != EExperimentStatus.Running && experiment.Status != EExperimentStatus.Paused)
            throw new UnprocessableEntityException($"Cannot complete experiment in '{experiment.Status}' status. Only Running or Paused experiments can be completed.");

        experiment.Status = EExperimentStatus.Completed;
        experiment.Outcome = request.Outcome;
        experiment.OutcomeComment = request.Comment;
        experiment.CompletedAt = DateTime.UtcNow;
        experiment.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync($"✅ Эксперимент завершён: {experiment.Name} | Исход: {request.Outcome}", cancellationToken);

        if (request.Outcome == ECompletionOutcome.RolloutWinner && request.WinnerVariantId.HasValue)
        {
            var winner = await dbContext.Variants
                .FirstOrDefaultAsync(v => v.Id == request.WinnerVariantId && v.ExperimentId == experiment.Id, cancellationToken);

            if (winner == null)
                throw new NotFoundException($"Variant with ID '{request.WinnerVariantId}' not found in experiment '{experiment.Id}'");

            var featureFlag = await dbContext.FeatureFlags
                .FirstOrDefaultAsync(f => f.Id == experiment.FeatureFlagId, cancellationToken);

            if (featureFlag != null)
            {
                featureFlag.DefaultValue = winner.Value;
                featureFlag.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                await cache.RemoveAsync($"flag:{featureFlag.Key}", cancellationToken);
                await notificationService.NotifyAsync(
                    $"🏆 Победитель раскатан: {experiment.Name} | вариант: {winner.Name} = {winner.Value}",
                    cancellationToken);
            }
        }

        return experiment;
    }
}