using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Autopilot;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Autopilot;

public class GetRampPlanHistoryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetRampPlanHistoryQuery, List<RampPlanHistoryEntity>>
{
    public async Task<List<RampPlanHistoryEntity>> Handle(GetRampPlanHistoryQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.RampPlanHistory
            .Where(h => h.ExperimentId == request.ExperimentId)
            .OrderByDescending(h => h.Timestamp)
            .ToListAsync(cancellationToken);
    }
}