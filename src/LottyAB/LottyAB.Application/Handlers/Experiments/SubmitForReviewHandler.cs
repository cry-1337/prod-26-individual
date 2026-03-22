using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Experiments;

public class SubmitForReviewHandler(IApplicationDbContext dbContext, INotificationService notificationService)
    : IRequestHandler<SubmitForReviewCommand, ExperimentEntity>
{
    public async Task<ExperimentEntity> Handle(SubmitForReviewCommand request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .FirstOrDefaultAsync(e => e.Id == request.ExperimentId, cancellationToken);

        if (experiment == null)
            throw new NotFoundException($"Experiment with ID '{request.ExperimentId}' not found");

        if (experiment.Status != EExperimentStatus.Draft)
            throw new UnprocessableEntityException($"Cannot submit experiment in '{experiment.Status}' status. Only Draft experiments can be submitted for review.");

        experiment.Status = EExperimentStatus.InReview;
        experiment.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync($"📋 Эксперимент отправлен на ревью: {experiment.Name}", cancellationToken);

        return experiment;
    }
}