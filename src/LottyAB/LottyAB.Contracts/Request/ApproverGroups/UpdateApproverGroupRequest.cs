namespace LottyAB.Contracts.Request.ApproverGroups;

public record UpdateApproverGroupRequest(string Name, string? Description, int ApproversToStart, List<Guid>? ApproverIds);