using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Experiments;

public class PauseExperimentHandler(IApplicationDbContext dbContext)
    : IRequestHandler<PauseExperimentCommand, ExperimentEntity>
{
    public async Task<ExperimentEntity> Handle(PauseExperimentCommand request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .FirstOrDefaultAsync(e => e.Id == request.ExperimentId, cancellationToken);

        if (experiment == null)
            throw new NotFoundException($"Experiment with ID '{request.ExperimentId}' not found");

        if (experiment.Status != EExperimentStatus.Running)
            throw new UnprocessableEntityException($"Cannot pause experiment in '{experiment.Status}' status. Only Running experiments can be paused.");

        experiment.Status = EExperimentStatus.Paused;
        experiment.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return experiment;
    }
}