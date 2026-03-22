using System.Text.Json;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LottyAB.Infrastructure.Services;

public class AutopilotRampService(
    IServiceProvider serviceProvider,
    ILogger<AutopilotRampService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Autopilot Ramp Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckRampPlans(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Autopilot Ramp Service");
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }

    internal async Task CheckRampPlans(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var rampPlans = await dbContext.RampPlans
            .Include(rp => rp.Experiment).ThenInclude(e => e.FeatureFlag)
            .Include(rp => rp.Experiment).ThenInclude(e => e.Variants)
            .Where(rp => rp.IsEnabled && !rp.IsCompleted
                && rp.Experiment.Status == EExperimentStatus.Running)
            .ToListAsync(ct);

        foreach (var rampPlan in rampPlans)
        {
            await ProcessExperiment(dbContext, cache, notificationService, rampPlan.Experiment, rampPlan, ct);
        }
    }

    private static async Task ProcessExperiment(
        IApplicationDbContext dbContext,
        IDistributedCache cache,
        INotificationService notificationService,
        ExperimentEntity experiment,
        RampPlanEntity rampPlan,
        CancellationToken ct)
    {
        var steps = JsonSerializer.Deserialize<double[]>(rampPlan.StepsJson)!;

        if (rampPlan.CurrentStepIndex >= steps.Length)
        {
            rampPlan.IsCompleted = true;
            dbContext.RampPlanHistory.Add(new RampPlanHistoryEntity
            {
                RampPlanId = rampPlan.Id,
                ExperimentId = experiment.Id,
                Action = ERampPlanAction.Completed,
                FromFraction = experiment.AudienceFraction,
                ToFraction = experiment.AudienceFraction,
                Reason = "All steps completed",
                Timestamp = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(ct);
            await notificationService.NotifyAsync(
                $"✅ Автопилот: {experiment.Name} | Все шаги раскатки завершены", ct);
            return;
        }

        var hasTriggers = await dbContext.GuardrailTriggerHistory
            .AnyAsync(h => h.ExperimentId == experiment.Id && h.TriggeredAt >= rampPlan.StepEnteredAt, ct);

        if (hasTriggers)
        {
            await ApplySafetyAction(dbContext, cache, notificationService, experiment, rampPlan, steps, ct);
            return;
        }

        if ((DateTime.UtcNow - rampPlan.StepEnteredAt).TotalMinutes < rampPlan.MinMinutesPerStep)
            return;

        var impressionCount = await dbContext.Decisions
            .CountAsync(d => d.ExperimentId == experiment.Id && d.Timestamp >= rampPlan.StepEnteredAt, ct);

        if (impressionCount < rampPlan.MinImpressionsPerStep)
            return;

        var nextFraction = steps[rampPlan.CurrentStepIndex];
        var oldFraction = experiment.AudienceFraction;

        foreach (var variant in experiment.Variants)
            variant.Weight = variant.Weight / oldFraction * nextFraction;

        experiment.AudienceFraction = nextFraction;
        experiment.Version++;
        experiment.UpdatedAt = DateTime.UtcNow;

        var variantsSnapshot = experiment.Variants.Select(v => new
        {
            v.Name,
            v.Value,
            v.Weight,
            v.IsControl
        }).ToList();

        dbContext.ExperimentVersions.Add(new ExperimentVersionEntity
        {
            ExperimentId = experiment.Id,
            Version = experiment.Version - 1,
            Name = experiment.Name,
            Description = experiment.Description,
            AudienceFraction = oldFraction,
            TargetingRule = experiment.TargetingRule,
            PrimaryMetricKey = experiment.PrimaryMetricKey,
            GuardrailMetricKeys = experiment.GuardrailMetricKeys,
            Status = experiment.Status,
            VariantsSnapshot = JsonSerializer.Serialize(variantsSnapshot),
            ChangedBy = Guid.Empty,
            ChangeReason = "Autopilot ramp advance"
        });

        rampPlan.CurrentStepIndex++;
        rampPlan.StepEnteredAt = DateTime.UtcNow;
        dbContext.RampPlanHistory.Add(new RampPlanHistoryEntity
        {
            RampPlanId = rampPlan.Id,
            ExperimentId = experiment.Id,
            Action = ERampPlanAction.Advanced,
            FromFraction = oldFraction,
            ToFraction = nextFraction,
            Reason = "All gates passed",
            Timestamp = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(ct);
        await cache.RemoveAsync($"flag:{experiment.FeatureFlag.Key}", ct);
        await notificationService.NotifyAsync(
            $"📈 Автопилот: {experiment.Name} | {oldFraction * 100:F0}% -> {nextFraction * 100:F0}%", ct);
    }

    private static async Task ApplySafetyAction(
        IApplicationDbContext dbContext,
        IDistributedCache cache,
        INotificationService notificationService,
        ExperimentEntity experiment,
        RampPlanEntity rampPlan,
        double[] steps,
        CancellationToken ct)
    {
        var currentFraction = experiment.AudienceFraction;

        switch (rampPlan.SafetyAction)
        {
            case ERampSafetyAction.Pause:
                experiment.Status = EExperimentStatus.Paused;
                dbContext.RampPlanHistory.Add(new RampPlanHistoryEntity
                {
                    RampPlanId = rampPlan.Id,
                    ExperimentId = experiment.Id,
                    Action = ERampPlanAction.Paused,
                    FromFraction = currentFraction,
                    ToFraction = currentFraction,
                    Reason = "Guardrail triggered: pausing experiment",
                    Timestamp = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync(ct);
                await cache.RemoveAsync($"flag:{experiment.FeatureFlag.Key}", ct);
                await notificationService.NotifyAsync(
                    $"⏸ Автопилот: {experiment.Name} | Остановлен из-за Guardrail", ct);
                break;

            case ERampSafetyAction.RollbackToControl:
                experiment.Status = EExperimentStatus.Completed;
                experiment.Outcome = ECompletionOutcome.Rollback;
                experiment.OutcomeComment = "Autopilot rollback to control due to guardrail trigger";
                experiment.CompletedAt = DateTime.UtcNow;
                dbContext.RampPlanHistory.Add(new RampPlanHistoryEntity
                {
                    RampPlanId = rampPlan.Id,
                    ExperimentId = experiment.Id,
                    Action = ERampPlanAction.RolledBack,
                    FromFraction = currentFraction,
                    ToFraction = 0,
                    Reason = "Guardrail triggered: rollback to control",
                    Timestamp = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync(ct);
                await cache.RemoveAsync($"flag:{experiment.FeatureFlag.Key}", ct);
                await notificationService.NotifyAsync(
                    $"🚨 Автопилот: {experiment.Name} | Откатился до контрольной версии из-за Guardrail", ct);
                break;

            default:
            case ERampSafetyAction.StepBack:
                if (rampPlan.CurrentStepIndex > 0)
                {
                    double newFraction;
                    if (rampPlan.CurrentStepIndex >= 2)
                        newFraction = steps[rampPlan.CurrentStepIndex - 2];
                    else
                        newFraction = steps[0] / 2.0;

                    foreach (var variant in experiment.Variants)
                        variant.Weight = variant.Weight / currentFraction * newFraction;

                    experiment.AudienceFraction = newFraction;
                    experiment.UpdatedAt = DateTime.UtcNow;
                    rampPlan.CurrentStepIndex--;
                    rampPlan.StepEnteredAt = DateTime.UtcNow;
                    dbContext.RampPlanHistory.Add(new RampPlanHistoryEntity
                    {
                        RampPlanId = rampPlan.Id,
                        ExperimentId = experiment.Id,
                        Action = ERampPlanAction.SteppedBack,
                        FromFraction = currentFraction,
                        ToFraction = newFraction,
                        Reason = "Guardrail triggered: stepping back",
                        Timestamp = DateTime.UtcNow
                    });
                    await dbContext.SaveChangesAsync(ct);
                    await cache.RemoveAsync($"flag:{experiment.FeatureFlag.Key}", ct);
                    await notificationService.NotifyAsync(
                        $"⬇ Автопилот: {experiment.Name} | Откатился: {currentFraction * 100:F0}% -> {newFraction * 100:F0}%", ct);
                }
                else
                {
                    experiment.Status = EExperimentStatus.Paused;
                    dbContext.RampPlanHistory.Add(new RampPlanHistoryEntity
                    {
                        RampPlanId = rampPlan.Id,
                        ExperimentId = experiment.Id,
                        Action = ERampPlanAction.Paused,
                        FromFraction = currentFraction,
                        ToFraction = currentFraction,
                        Reason = "Guardrail triggered: cannot step back from index 0, pausing",
                        Timestamp = DateTime.UtcNow
                    });
                    await dbContext.SaveChangesAsync(ct);
                    await cache.RemoveAsync($"flag:{experiment.FeatureFlag.Key}", ct);
                    await notificationService.NotifyAsync(
                        $"⏸ Автопилот: {experiment.Name} | Остановлен (Нету версий, на которые можно вернуться)", ct);
                }
                break;
        }
    }
}