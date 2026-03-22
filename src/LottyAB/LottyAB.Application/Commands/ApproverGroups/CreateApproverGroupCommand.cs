using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Commands.ApproverGroups;

public record CreateApproverGroupCommand(string Name, string? Description, int ApproversToStart, List<Guid>? ApproverIds) : IRequest<ApproverGroupEntity>;