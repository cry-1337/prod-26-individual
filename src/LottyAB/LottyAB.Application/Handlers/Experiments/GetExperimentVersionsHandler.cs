using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Experiments;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Experiments;

public class GetExperimentVersionsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetExperimentVersionsQuery, List<ExperimentVersionEntity>>
{
    public async Task<List<ExperimentVersionEntity>> Handle(GetExperimentVersionsQuery request, CancellationToken cancellationToken)
    {
        var experimentExists = await dbContext.Experiments
            .AnyAsync(e => e.Id == request.ExperimentId, cancellationToken);

        if (!experimentExists)
            throw new NotFoundException($"Experiment with ID '{request.ExperimentId}' not found");

        var versions = await dbContext.ExperimentVersions
            .Where(v => v.ExperimentId == request.ExperimentId)
            .OrderByDescending(v => v.Version)
            .ToListAsync(cancellationToken);

        return versions;
    }
}