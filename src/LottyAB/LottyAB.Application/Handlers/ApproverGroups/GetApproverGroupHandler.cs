using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.ApproverGroups;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.ApproverGroups;

public class GetApproverGroupHandler(IApplicationDbContext dbContext) : IRequestHandler<GetApproverGroupQuery, ApproverGroupEntity>
{
    public async Task<ApproverGroupEntity> Handle(GetApproverGroupQuery request, CancellationToken cancellationToken)
    {
        var approverGroup = await dbContext.ApproverGroups
            .Include(g => g.Approvers)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (approverGroup == null)
            throw new NotFoundException("ApproverGroup", request.Id);

        return approverGroup;
    }
}