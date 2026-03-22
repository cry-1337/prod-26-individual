using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Guardrails;
using LottyAB.Contracts.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Guardrails;

public class GetGuardrailsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetGuardrailsQuery, List<GuardrailResponse>>
{
    public async Task<List<GuardrailResponse>> Handle(GetGuardrailsQuery request, CancellationToken cancellationToken)
    {
        var guardrails = await dbContext.Guardrails
            .Where(g => g.ExperimentId == request.ExperimentId && g.IsActive)
            .OrderBy(g => g.MetricKey)
            .ToListAsync(cancellationToken);

        return guardrails.Select(g => new GuardrailResponse
        {
            Id = g.Id,
            MetricKey = g.MetricKey,
            Threshold = g.Threshold,
            ObservationWindowMinutes = g.ObservationWindowMinutes,
            Action = g.Action,
            IsActive = g.IsActive
        }).ToList();
    }
}