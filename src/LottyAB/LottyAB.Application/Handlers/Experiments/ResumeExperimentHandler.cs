using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Experiments;

public class ResumeExperimentHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ResumeExperimentCommand, ExperimentEntity>
{
    public async Task<ExperimentEntity> Handle(ResumeExperimentCommand request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .FirstOrDefaultAsync(e => e.Id == request.ExperimentId, cancellationToken);

        if (experiment == null)
            throw new NotFoundException($"Experiment with ID '{request.ExperimentId}' not found");

        if (experiment.Status != EExperimentStatus.Paused)
            throw new UnprocessableEntityException($"Cannot resume experiment in '{experiment.Status}' status. Only Paused experiments can be resumed.");

        experiment.Status = EExperimentStatus.Running;
        experiment.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return experiment;
    }
}