using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Experiments;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Experiments;

public class GetExperimentByIdHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetExperimentByIdQuery, ExperimentEntity>
{
    public async Task<ExperimentEntity> Handle(GetExperimentByIdQuery request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .Include(e => e.FeatureFlag)
            .Include(e => e.Variants)
            .Include(e => e.Reviews)
                .ThenInclude(r => r.Reviewer)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);

        return experiment ?? throw new NotFoundException($"Experiment with ID '{request.Id}' not found");
    }
}