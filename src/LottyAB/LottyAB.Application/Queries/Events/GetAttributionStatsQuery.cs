using MediatR;

namespace LottyAB.Application.Queries.Events;

public record GetAttributionStatsQuery : IRequest<AttributionStatsResponse>;

public record AttributionStatsResponse(
    int TotalEvents,
    int AttributedEvents,
    int UnattributedEvents,
    int EventsWaitingForExposure,
    int ExposureEvents,
    double AttributionRate,
    DateTime? OldestUnattributedEventTime);