using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Events;

public class GetAttributionStatsHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetAttributionStatsQuery, AttributionStatsResponse>
{
    public async Task<AttributionStatsResponse> Handle(GetAttributionStatsQuery request, CancellationToken cancellationToken)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

        var recentEvents = await dbContext.Events
            .Include(e => e.EventType)
            .Where(e => e.EventTimestamp >= sevenDaysAgo)
            .Select(e => new
            {
                e.IsAttributed,
                e.EventType.RequiresExposure,
                e.EventType.IsExposureEvent,
                e.EventTimestamp
            })
            .ToListAsync(cancellationToken);

        var totalEvents = recentEvents.Count;
        var attributedEvents = recentEvents.Count(e => e.IsAttributed);
        var unattributedEvents = totalEvents - attributedEvents;
        var exposureEvents = recentEvents.Count(e => e.IsExposureEvent);

        var eventsWaitingForExposure = recentEvents.Count(e =>
            !e.IsAttributed && e.RequiresExposure && !e.IsExposureEvent);

        var oldestUnattributed = recentEvents
            .Where(e => !e.IsAttributed && e.RequiresExposure && !e.IsExposureEvent)
            .MinBy(e => e.EventTimestamp);

        var attributionRate = totalEvents > 0 ? (double)attributedEvents / totalEvents : 0.0;

        return new AttributionStatsResponse(
            totalEvents,
            attributedEvents,
            unattributedEvents,
            eventsWaitingForExposure,
            exposureEvents,
            attributionRate,
            oldestUnattributed?.EventTimestamp);
    }
}