using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Events;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Events.Types;

public class GetEventTypesHandler(IApplicationDbContext dbContext) : IRequestHandler<GetEventTypesQuery, List<EventTypeEntity>>
{
    public async Task<List<EventTypeEntity>> Handle(GetEventTypesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.EventTypes
            .Where(et => !et.IsArchived)
            .OrderBy(et => et.EventKey)
            .ToListAsync(cancellationToken);
    }
}