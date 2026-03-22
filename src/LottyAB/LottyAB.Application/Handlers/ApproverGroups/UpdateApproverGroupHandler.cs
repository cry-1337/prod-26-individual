using LottyAB.Application.Commands.ApproverGroups;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.ApproverGroups;

public class UpdateApproverGroupHandler(IApplicationDbContext dbContext) : IRequestHandler<UpdateApproverGroupCommand, ApproverGroupEntity>
{
    public async Task<ApproverGroupEntity> Handle(UpdateApproverGroupCommand request, CancellationToken cancellationToken)
    {
        var approverGroup = await dbContext.ApproverGroups
            .Include(g => g.Approvers)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (approverGroup == null)
            throw new NotFoundException("ApproverGroup", request.Id);

        approverGroup.Name = request.Name;
        approverGroup.Description = request.Description;
        approverGroup.ApproversToStart = request.ApproversToStart;
        approverGroup.UpdatedAt = DateTime.UtcNow;

        if (request.ApproverIds != null)
        {
            var newApprovers = await dbContext.Users
                .Where(u => request.ApproverIds.Contains(u.Id))
                .ToListAsync(cancellationToken);
            approverGroup.Approvers = newApprovers;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return approverGroup;
    }
}