using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Experiments;

public class ReviewExperimentHandler(IApplicationDbContext dbContext, INotificationService notificationService)
    : IRequestHandler<ReviewExperimentCommand, ExperimentEntity>
{
    public async Task<ExperimentEntity> Handle(ReviewExperimentCommand request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .Include(e => e.Reviews)
            .Include(e => e.Owner)
            .Include(e => e.ApproverGroup)
                .ThenInclude(g => g!.Approvers)
            .FirstOrDefaultAsync(e => e.Id == request.ExperimentId, cancellationToken);

        if (experiment == null)
            throw new NotFoundException($"Experiment with ID '{request.ExperimentId}' not found");

        if (experiment.Status != EExperimentStatus.InReview)
            throw new UnprocessableEntityException($"Cannot review experiment in '{experiment.Status}' status. Only experiments InReview can be reviewed.");

        var alreadyReviewed = experiment.Reviews.Any(r => r.ReviewerId == request.ReviewerId);
        if (alreadyReviewed)
            throw new UnprocessableEntityException("Reviewer has already submitted a review for this experiment.");

        var approverGroup = experiment.ApproverGroup;
        if (approverGroup != null)
        {
            var isInGroup = approverGroup.Approvers.Any(a => a.Id == request.ReviewerId);
            if (!isInGroup)
                throw new UnprocessableEntityException("Reviewer is not in the approver group assigned to this experiment.");
        }

        var review = new ExperimentReviewEntity
        {
            ExperimentId = experiment.Id,
            ReviewerId = request.ReviewerId,
            Decision = request.Decision,
            Comment = request.Comment
        };

        switch (request.Decision)
        {
            case EReviewDecision.ChangesRequested:
                experiment.Status = EExperimentStatus.Draft;
                break;

            case EReviewDecision.Rejected:
                experiment.Status = EExperimentStatus.Rejected;
                break;

            case EReviewDecision.Approved:
                var approvalCount = experiment.Reviews.Count(r => r.Decision == EReviewDecision.Approved) + 1;
                var threshold = approverGroup?.ApproversToStart ?? 1;
                if (approvalCount >= threshold)
                    experiment.Status = EExperimentStatus.Approved;
                break;

            default:
                throw new UnprocessableEntityException("Unknown decision");
        }

        dbContext.ExperimentReviews.Add(review);

        experiment.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (experiment.Status == EExperimentStatus.Approved)
            await notificationService.NotifyAsync($"✅ Эксперимент одобрен: {experiment.Name}", cancellationToken);

        return experiment;
    }
}