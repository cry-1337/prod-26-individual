using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Autopilot;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Autopilot;

public class GetRampPlanHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetRampPlanQuery, RampPlanEntity>
{
    public async Task<RampPlanEntity> Handle(GetRampPlanQuery request, CancellationToken cancellationToken)
    {
        var plan = await dbContext.RampPlans
            .Include(rp => rp.History)
            .FirstOrDefaultAsync(rp => rp.ExperimentId == request.ExperimentId, cancellationToken);

        if (plan == null)
            throw new NotFoundException($"Autopilot ramp plan for experiment '{request.ExperimentId}' not found.");

        return plan;
    }
}