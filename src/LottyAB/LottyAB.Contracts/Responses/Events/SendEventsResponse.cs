namespace LottyAB.Contracts.Responses.Events;

public record SendEventsResponse(int Accepted, int Duplicates, int Rejected, List<EventRejection> Rejections);
public record EventRejection(string EventId, string Reason);