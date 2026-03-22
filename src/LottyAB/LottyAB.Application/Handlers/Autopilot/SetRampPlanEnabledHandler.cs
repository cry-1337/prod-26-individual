using LottyAB.Application.Commands.Autopilot;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Autopilot;

public class SetRampPlanEnabledHandler(IApplicationDbContext dbContext)
    : IRequestHandler<SetRampPlanEnabledCommand, RampPlanEntity>
{
    public async Task<RampPlanEntity> Handle(SetRampPlanEnabledCommand request, CancellationToken cancellationToken)
    {
        var plan = await dbContext.RampPlans
            .FirstOrDefaultAsync(rp => rp.ExperimentId == request.ExperimentId, cancellationToken);

        if (plan == null)
            throw new NotFoundException($"Autopilot ramp plan for experiment '{request.ExperimentId}' not found.");

        plan.IsEnabled = request.IsEnabled;
        plan.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return plan;
    }
}