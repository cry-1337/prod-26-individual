namespace LottyAB.Contracts.Request.ApproverGroups;

public record CreateApproverGroupRequest(string Name, string? Description, int ApproversToStart, List<Guid>? ApproverIds);