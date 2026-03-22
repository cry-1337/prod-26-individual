using MediatR;

namespace LottyAB.Application.Commands.ApproverGroups;

public record DeleteApproverGroupCommand(Guid Id) : IRequest<Unit>;