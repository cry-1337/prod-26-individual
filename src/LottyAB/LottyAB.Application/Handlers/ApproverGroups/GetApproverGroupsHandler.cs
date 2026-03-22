using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.ApproverGroups;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.ApproverGroups;

public class GetApproverGroupsHandler(IApplicationDbContext dbContext) : IRequestHandler<GetApproverGroupsQuery, PagedResponse<ApproverGroupEntity>>
{
    public async Task<PagedResponse<ApproverGroupEntity>> Handle(GetApproverGroupsQuery request, CancellationToken cancellationToken)
    {
        var approverGroups = await dbContext.ApproverGroups
            .Include(g => g.Approvers)
            .Skip(request.Page * request.Size)
            .Take(request.Size)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return new PagedResponse<ApproverGroupEntity>
        {
            Items = approverGroups,
            TotalCount = await dbContext.ApproverGroups.CountAsync(cancellationToken),
            PageNumber = request.Page,
            PageSize = request.Size
        };
    }
}