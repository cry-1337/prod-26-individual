using LottyAB.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LottyAB.Infrastructure.Services;

public class EventAttributionService(
    IServiceProvider serviceProvider,
    ILogger<EventAttributionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Event Attribution Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessUnattributedEvents(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Event Attribution Service");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }

    internal async Task ProcessUnattributedEvents(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var unattributedEvents = await dbContext.Events
            .Include(e => e.EventType)
            .Where(e => !e.IsAttributed
                        && e.EventType.RequiresExposure
                        && !e.EventType.IsExposureEvent
                        && e.EventTimestamp >= sevenDaysAgo)
            .Take(1000)
            .ToListAsync(cancellationToken);

        if (unattributedEvents.Count == 0)
            return;

        logger.LogDebug("Processing {Count} unattributed events", unattributedEvents.Count);

        var decisionIds = unattributedEvents.Select(e => e.DecisionId).Distinct().ToList();

        var exposureEventTypeIds = await dbContext.EventTypes
            .Where(et => et.IsExposureEvent)
            .Select(et => et.Id)
            .ToListAsync(cancellationToken);

        var exposures = await dbContext.Events
            .Where(e => decisionIds.Contains(e.DecisionId) && exposureEventTypeIds.Contains(e.EventTypeId))
            .Select(e => e.DecisionId)
            .ToListAsync(cancellationToken);

        var exposureSet = exposures.ToHashSet();

        var oldestWaitingEvent = DateTime.UtcNow;
        foreach (var evt in unattributedEvents)
        {
            if (exposureSet.Contains(evt.DecisionId))
                evt.IsAttributed = true;
            else if (evt.EventTimestamp < oldestWaitingEvent)
                oldestWaitingEvent = evt.EventTimestamp;
        }

        var attributed = unattributedEvents.Count(e => e.IsAttributed);
        var stillWaiting = unattributedEvents.Count - attributed;

        if (attributed > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Event attribution completed: {Attributed} attributed, {Waiting} still waiting for exposure. Oldest waiting event: {OldestAge:0.0}h ago",
                attributed,
                stillWaiting,
                (DateTime.UtcNow - oldestWaitingEvent).TotalHours);
        }
        else if (stillWaiting > 0)
        {
            logger.LogWarning(
                "{Count} events waiting for exposure. Oldest event: {OldestAge:0.0}h ago. Check if exposure events are being sent.",
                stillWaiting,
                (DateTime.UtcNow - oldestWaitingEvent).TotalHours);
        }
    }
}