using LottyAB.Application.Commands.ApproverGroups;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.ApproverGroups;

public class CreateApproverGroupHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateApproverGroupCommand, ApproverGroupEntity>
{
    public async Task<ApproverGroupEntity> Handle(CreateApproverGroupCommand request, CancellationToken cancellationToken)
    {
        var approverGroup = new ApproverGroupEntity
        {
            Name = request.Name,
            Description = request.Description,
            ApproversToStart = request.ApproversToStart
        };

        if (request.ApproverIds is { Count: > 0 })
        {
            var approvers = await dbContext.Users
                .Where(u => request.ApproverIds.Contains(u.Id))
                .ToListAsync(cancellationToken);
            approverGroup.Approvers = approvers;
        }

        await dbContext.ApproverGroups.AddAsync(approverGroup, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return approverGroup;
    }
}