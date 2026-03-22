using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LottyAB.Infrastructure.Services;

public class GuardrailMonitoringService(
    IServiceProvider serviceProvider,
    ILogger<GuardrailMonitoringService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Guardrail Monitoring Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckGuardrails(stoppingToken);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Guardrail Monitoring Service");
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }

    internal async Task CheckGuardrails(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var metricCalculator = scope.ServiceProvider.GetRequiredService<IMetricCalculator>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var runningExperiments = await dbContext.Experiments
            .Include(e => e.Guardrails)
            .Where(e => e.Status == EExperimentStatus.Running)
            .ToListAsync(cancellationToken);

        foreach (var experiment in runningExperiments)
        {
            var activeGuardrails = experiment.Guardrails.Where(g => g.IsActive).ToList();
            if (activeGuardrails.Count == 0)
                continue;

            foreach (var guardrail in activeGuardrails)
            {
                try
                {
                    await CheckSingleGuardrail(dbContext, metricCalculator, notificationService, experiment, guardrail, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "Error checking guardrail {GuardrailId} for experiment {ExperimentId}",
                        guardrail.Id, experiment.Id);
                }
            }
        }
    }

    private async Task CheckSingleGuardrail(
        IApplicationDbContext dbContext,
        IMetricCalculator metricCalculator,
        INotificationService notificationService,
        ExperimentEntity experiment,
        GuardrailEntity guardrail,
        CancellationToken cancellationToken)
    {
        var windowStart = DateTime.UtcNow.AddMinutes(-guardrail.ObservationWindowMinutes);

        var decisions = await dbContext.Decisions
            .Where(d => d.ExperimentId == experiment.Id && d.Timestamp >= windowStart)
            .ToListAsync(cancellationToken);

        if (decisions.Count == 0)
        {
            logger.LogDebug(
                "No decisions in observation window for experiment {ExperimentId}, guardrail {GuardrailId}",
                experiment.Id, guardrail.Id);
            return;
        }

        var decisionIds = decisions.Select(d => d.Id).ToList();

        var events = await dbContext.Events
            .Include(e => e.EventType)
            .Where(e => decisionIds.Contains(e.DecisionId) && e.IsAttributed)
            .ToListAsync(cancellationToken);

        var eventCounts = events
            .GroupBy(e => e.EventType.EventKey)
            .ToDictionary(g => g.Key, g => g.Count());

        var metricDefinitions = await metricCalculator.LoadMetricDefinitionsAsync([guardrail.MetricKey], cancellationToken);

        if (metricDefinitions.Count == 0)
        {
            logger.LogWarning(
                "Metric definition not found for guardrail {GuardrailId}, metric key: {MetricKey}",
                guardrail.Id, guardrail.MetricKey);
            return;
        }

        var metricValues = metricCalculator.CalculateMetrics(metricDefinitions, eventCounts);

        if (!metricValues.TryGetValue(guardrail.MetricKey, out var actualValue))
        {
            logger.LogDebug(
                "Metric value not calculated for guardrail {GuardrailId}, metric key: {MetricKey}",
                guardrail.Id, guardrail.MetricKey);
            return;
        }

        if (actualValue > guardrail.Threshold)
        {
            logger.LogWarning(
                "Guardrail TRIGGERED! Experiment {ExperimentId}, Metric: {MetricKey}, Threshold: {Threshold}, Actual: {ActualValue}",
                experiment.Id, guardrail.MetricKey, guardrail.Threshold, actualValue);

            await TriggerGuardrail(dbContext, notificationService, experiment, guardrail, actualValue, cancellationToken);
        }
        else
        {
            logger.LogDebug(
                "Guardrail check OK. Experiment {ExperimentId}, Metric: {MetricKey}, Threshold: {Threshold}, Actual: {ActualValue}",
                experiment.Id, guardrail.MetricKey, guardrail.Threshold, actualValue);
        }
    }

    private async Task TriggerGuardrail(
        IApplicationDbContext dbContext,
        INotificationService notificationService,
        ExperimentEntity experiment,
        GuardrailEntity guardrail,
        double actualValue,
        CancellationToken cancellationToken)
    {
        var triggerHistory = new GuardrailTriggerHistoryEntity
        {
            GuardrailId = guardrail.Id,
            ExperimentId = experiment.Id,
            MetricKey = guardrail.MetricKey,
            Threshold = guardrail.Threshold,
            ActualValue = actualValue,
            ActionTaken = guardrail.Action,
            TriggeredAt = DateTime.UtcNow,
            ObservationWindowMinutes = guardrail.ObservationWindowMinutes
        };

        dbContext.GuardrailTriggerHistory.Add(triggerHistory);

        switch (guardrail.Action)
        {
            default:
            case EGuardrailAction.Pause:
                experiment.Status = EExperimentStatus.Paused;
                logger.LogWarning(
                    "Guardrail action: PAUSED experiment {ExperimentId} due to {MetricKey} exceeding threshold ({ActualValue} > {Threshold})",
                    experiment.Id, guardrail.MetricKey, actualValue, guardrail.Threshold);
                break;
            case EGuardrailAction.RollbackToControl:
                experiment.Status = EExperimentStatus.Completed;
                experiment.Outcome = ECompletionOutcome.Rollback;
                experiment.OutcomeComment =
                    $"Automatically rolled back by guardrail. {guardrail.MetricKey} exceeded threshold: {actualValue:F2} > {guardrail.Threshold:F2}";
                experiment.CompletedAt = DateTime.UtcNow;
                logger.LogWarning(
                    "Guardrail action: ROLLED BACK experiment {ExperimentId} to control due to {MetricKey} exceeding threshold ({ActualValue} > {Threshold})",
                    experiment.Id, guardrail.MetricKey, actualValue, guardrail.Threshold);
                break;
        }

        guardrail.IsActive = false;

        await dbContext.SaveChangesAsync(cancellationToken);

        var notificationMessage = guardrail.Action switch
        {
            EGuardrailAction.RollbackToControl =>
                $"🚨 <b>Guardrail: откат к контролю</b>\nЭксперимент: {experiment.Name}\nМетрика: {guardrail.MetricKey} = {actualValue:F2} (порог: {guardrail.Threshold:F2})",
            _ =>
                $"🚨 <b>Guardrail: эксперимент приостановлен</b>\nЭксперимент: {experiment.Name}\nМетрика: {guardrail.MetricKey} = {actualValue:F2} (порог: {guardrail.Threshold:F2})"
        };
        await notificationService.NotifyAsync(notificationMessage, cancellationToken);

        logger.LogInformation(
            "Guardrail trigger recorded. Experiment: {ExperimentId}, Action: {Action}, History ID: {HistoryId}",
            experiment.Id, guardrail.Action, triggerHistory.Id);
    }
}