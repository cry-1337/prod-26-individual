using System.Text.Json;
using LottyAB.Application.Commands.Autopilot;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Autopilot;

public class CreateRampPlanHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateRampPlanCommand, RampPlanEntity>
{
    public async Task<RampPlanEntity> Handle(CreateRampPlanCommand request, CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .Include(e => e.RampPlan)
            .FirstOrDefaultAsync(e => e.Id == request.ExperimentId, cancellationToken);

        if (experiment == null)
            throw new NotFoundException($"Experiment with ID '{request.ExperimentId}' not found");

        if (experiment.Status != EExperimentStatus.Running)
            throw new UnprocessableEntityException(
                $"Cannot create autopilot plan for experiment in '{experiment.Status}' status. Only Running experiments are supported.");

        if (experiment.RampPlan != null)
            throw new ConflictException("An autopilot ramp plan already exists for this experiment.");

        if (request.Steps == null || request.Steps.Length == 0)
            throw new UnprocessableEntityException("Steps cannot be empty.");

        for (var i = 0; i < request.Steps.Length; i++)
        {
            var step = request.Steps[i];
            if (step < 0.05 || step > 1.0)
                throw new UnprocessableEntityException(
                    $"Step at index {i} ({step}) must be between 0.05 and 1.0.");

            if (i > 0 && step <= request.Steps[i - 1])
                throw new UnprocessableEntityException(
                    $"Steps must be strictly ascending. Step at index {i} ({step}) is not greater than step at index {i - 1} ({request.Steps[i - 1]}).");

            if (step <= experiment.AudienceFraction)
                throw new UnprocessableEntityException(
                    $"Step at index {i} ({step}) must be greater than current audience fraction ({experiment.AudienceFraction}).");
        }

        var plan = new RampPlanEntity
        {
            ExperimentId = request.ExperimentId,
            StepsJson = JsonSerializer.Serialize(request.Steps),
            CurrentStepIndex = 0,
            MinImpressionsPerStep = request.MinImpressionsPerStep,
            MinMinutesPerStep = request.MinMinutesPerStep,
            SafetyAction = request.SafetyAction,
            IsEnabled = true,
            IsCompleted = false,
            StepEnteredAt = DateTime.UtcNow
        };

        dbContext.RampPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        return plan;
    }
}