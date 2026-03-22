using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Experiments;

public class DeleteExperimentHandler(IApplicationDbContext dbContext)
    : IRequestHandler<DeleteExperimentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteExperimentCommand request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        if (experiment == null)
            throw new NotFoundException($"Experiment with ID '{request.Id}' not found");

        if (experiment.Status != EExperimentStatus.Draft && experiment.Status != EExperimentStatus.Rejected)
            throw new UnprocessableEntityException($"Cannot delete experiment in '{experiment.Status}' status. Only Draft or Rejected experiments can be deleted.");

        dbContext.Experiments.Remove(experiment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}