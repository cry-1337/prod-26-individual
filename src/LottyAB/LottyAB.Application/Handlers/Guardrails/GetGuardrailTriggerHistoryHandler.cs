using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Guardrails;
using LottyAB.Contracts.Responses;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Guardrails;

public class GetGuardrailTriggerHistoryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetGuardrailTriggerHistoryQuery, List<GuardrailTriggerHistoryResponse>>
{
    public async Task<List<GuardrailTriggerHistoryResponse>> Handle(
        GetGuardrailTriggerHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var history = await dbContext.GuardrailTriggerHistory
            .Where(h => h.ExperimentId == request.ExperimentId)
            .OrderByDescending(h => h.TriggeredAt)
            .ToListAsync(cancellationToken);

        return history.Select(h => new GuardrailTriggerHistoryResponse
        {
            Id = h.Id,
            GuardrailId = h.GuardrailId,
            ExperimentId = h.ExperimentId,
            MetricKey = h.MetricKey,
            Threshold = h.Threshold,
            ActualValue = h.ActualValue,
            ActionTaken = h.ActionTaken,
            TriggeredAt = h.TriggeredAt,
            ObservationWindowMinutes = h.ObservationWindowMinutes
        }).ToList();
    }
}