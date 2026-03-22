using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Experiments;

public class ArchiveExperimentHandler(IApplicationDbContext dbContext)
    : IRequestHandler<ArchiveExperimentCommand, ExperimentEntity>
{
    public async Task<ExperimentEntity> Handle(ArchiveExperimentCommand request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .FirstOrDefaultAsync(e => e.Id == request.ExperimentId, cancellationToken);

        if (experiment == null)
            throw new NotFoundException($"Experiment with ID '{request.ExperimentId}' not found");

        if (experiment.Status != EExperimentStatus.Completed)
            throw new UnprocessableEntityException($"Cannot archive experiment in '{experiment.Status}' status. Only Completed experiments can be archived.");

        experiment.Status = EExperimentStatus.Archived;
        experiment.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return experiment;
    }
}