using LottyAB.Application.Commands.Events;
using LottyAB.Application.Interfaces;
using LottyAB.Contracts.Responses.Events;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Events;

public class SendEventsHandler(IApplicationDbContext dbContext) : IRequestHandler<SendEventsCommand, SendEventsResponse>
{
    public async Task<SendEventsResponse> Handle(SendEventsCommand request, CancellationToken cancellationToken)
    {
        var accepted = 0;
        var duplicates = 0;
        var rejected = 0;
        var rejections = new List<EventRejection>();

        var eventTypeCache = await LoadEventTypesCache(request, cancellationToken);
        var existingEventIds = await LoadExistingEventIds(request, cancellationToken);
        var decisionIds = request.Request.Events.Select(e => e.DecisionId).Distinct().ToList();
        var existingDecisions = await LoadExistingDecisions(decisionIds, cancellationToken);

        foreach (var eventRequest in request.Request.Events)
        {
            if (existingEventIds.Contains(eventRequest.EventId))
            {
                duplicates++;
                continue;
            }

            if (!eventTypeCache.TryGetValue(eventRequest.EventTypeKey, out var eventType))
            {
                rejected++;
                rejections.Add(new EventRejection(eventRequest.EventId, $"Unknown event type: {eventRequest.EventTypeKey}"));
                continue;
            }

            if (!existingDecisions.ContainsKey(eventRequest.DecisionId))
            {
                rejected++;
                rejections.Add(new EventRejection(eventRequest.EventId, $"Decision not found: {eventRequest.DecisionId}"));
                continue;
            }

            var eventEntity = new EventEntity
            {
                EventId = eventRequest.EventId,
                EventTypeId = eventType.Id,
                DecisionId = eventRequest.DecisionId,
                SubjectId = eventRequest.SubjectId,
                EventTimestamp = eventRequest.EventTimestamp,
                ReceivedAt = DateTime.UtcNow,
                IsDuplicate = false,
                IsAttributed = !eventType.RequiresExposure || eventType.IsExposureEvent
            };

            eventEntity.SetProperties(eventRequest.Properties);

            dbContext.Events.Add(eventEntity);
            accepted++;
        }

        if (accepted > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new SendEventsResponse(accepted, duplicates, rejected, rejections);
    }

    private async Task<Dictionary<string, EventTypeEntity>> LoadEventTypesCache(
        SendEventsCommand request, CancellationToken cancellationToken)
    {
        var eventTypeKeys = request.Request.Events.Select(e => e.EventTypeKey).Distinct().ToList();
        var eventTypes = await dbContext.EventTypes
            .Where(et => eventTypeKeys.Contains(et.EventKey) && !et.IsArchived)
            .ToListAsync(cancellationToken);

        return eventTypes.ToDictionary(et => et.EventKey, et => et);
    }

    private async Task<HashSet<string>> LoadExistingEventIds(
        SendEventsCommand request, CancellationToken cancellationToken)
    {
        var eventIds = request.Request.Events.Select(e => e.EventId).ToList();
        var existing = await dbContext.Events
            .Where(e => eventIds.Contains(e.EventId))
            .Select(e => e.EventId)
            .ToListAsync(cancellationToken);

        return existing.ToHashSet();
    }

    private async Task<Dictionary<Guid, DecisionEntity>> LoadExistingDecisions(
        List<Guid> decisionIds, CancellationToken cancellationToken)
    {
        var decisions = await dbContext.Decisions
            .Where(d => decisionIds.Contains(d.Id))
            .ToListAsync(cancellationToken);

        return decisions.ToDictionary(d => d.Id, d => d);
    }
}