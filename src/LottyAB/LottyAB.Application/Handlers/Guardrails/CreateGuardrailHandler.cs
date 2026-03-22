using LottyAB.Application.Commands.Guardrails;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Guardrails;

public class CreateGuardrailHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateGuardrailCommand, GuardrailResponse>
{
    public async Task<GuardrailResponse> Handle(CreateGuardrailCommand request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .FirstOrDefaultAsync(e => e.Id == request.ExperimentId, cancellationToken);

        if (experiment == null)
            throw new NotFoundException($"Experiment with ID '{request.ExperimentId}' not found");

        if (experiment.Status != EExperimentStatus.Draft && experiment.Status != EExperimentStatus.Approved)
            throw new UnprocessableEntityException($"Cannot add guardrails to experiment in '{experiment.Status}' status");

        var metricExists = await dbContext.MetricDefinitions
            .AnyAsync(m => m.MetricKey == request.MetricKey && !m.IsArchived, cancellationToken);

        if (!metricExists)
            throw new NotFoundException($"Metric '{request.MetricKey}' not found");

        var guardrail = new GuardrailEntity
        {
            ExperimentId = request.ExperimentId,
            MetricKey = request.MetricKey,
            Threshold = request.Threshold,
            ObservationWindowMinutes = request.ObservationWindowMinutes,
            Action = request.Action,
            IsActive = true
        };

        dbContext.Guardrails.Add(guardrail);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new GuardrailResponse
        {
            Id = guardrail.Id,
            MetricKey = guardrail.MetricKey,
            Threshold = guardrail.Threshold,
            ObservationWindowMinutes = guardrail.ObservationWindowMinutes,
            Action = guardrail.Action,
            IsActive = guardrail.IsActive
        };
    }
}