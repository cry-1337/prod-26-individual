namespace LottyAB.Contracts.Request.Events;

public record SendEventRequest(
    string EventId,
    string EventTypeKey,
    Guid DecisionId,
    string SubjectId,
    Dictionary<string, object>? Properties,
    DateTime EventTimestamp);