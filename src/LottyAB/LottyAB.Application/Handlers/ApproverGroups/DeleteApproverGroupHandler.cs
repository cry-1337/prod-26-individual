using LottyAB.Application.Commands.ApproverGroups;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.ApproverGroups;

public class DeleteApproverGroupHandler(IApplicationDbContext dbContext) : IRequestHandler<DeleteApproverGroupCommand, Unit>
{
    public async Task<Unit> Handle(DeleteApproverGroupCommand request, CancellationToken cancellationToken)
    {
        var approverGroup = await dbContext.ApproverGroups
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (approverGroup == null)
            throw new NotFoundException("ApproverGroup", request.Id);

        dbContext.ApproverGroups.Remove(approverGroup);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}