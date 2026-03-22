using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Reports;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Reports;

public class GetMetricDefinitionsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetMetricDefinitionsQuery, List<MetricDefinitionEntity>>
{
    public async Task<List<MetricDefinitionEntity>> Handle(GetMetricDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        return await dbContext.MetricDefinitions
            .Where(m => !m.IsArchived)
            .OrderBy(m => m.MetricKey)
            .ToListAsync(cancellationToken);
    }
}