using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Events;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Events.Types;

public class GetEventTypeHandler(IApplicationDbContext dbContext) : IRequestHandler<GetEventTypeQuery, EventTypeEntity>
{
    public async Task<EventTypeEntity> Handle(GetEventTypeQuery request, CancellationToken cancellationToken)
    {
        var eventType = await dbContext.EventTypes
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (eventType == null) throw new NotFoundException("Event", request.Id);
        return eventType.IsArchived ? throw new UnprocessableEntityException("Event is archived") : eventType;
    }
}