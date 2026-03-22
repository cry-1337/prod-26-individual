namespace LottyAB.Contracts.Request.Events;

public record CreateEventTypeRequest(
    string EventKey,
    string DisplayName,
    string? Description,
    bool RequiresExposure,
    bool IsExposureEvent);