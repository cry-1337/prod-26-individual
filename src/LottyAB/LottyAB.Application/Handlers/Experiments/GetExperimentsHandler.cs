using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Experiments;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Experiments;

public class GetExperimentsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetExperimentsQuery, PagedResponse<ExperimentEntity>>
{
    public async Task<PagedResponse<ExperimentEntity>> Handle(GetExperimentsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Experiments
            .Include(e => e.FeatureFlag)
            .Include(e => e.Variants)
            .Include(e => e.Reviews)
            .AsQueryable();

        if (request.Status.HasValue)
            query = query.Where(e => e.Status == request.Status.Value);

        if (request.FeatureFlagId.HasValue)
            query = query.Where(e => e.FeatureFlagId == request.FeatureFlagId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var experiments = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ExperimentEntity>
        {
            Items = experiments,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }
}