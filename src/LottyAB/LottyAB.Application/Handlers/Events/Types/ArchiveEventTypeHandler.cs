using LottyAB.Application.Commands.Events;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Events.Types;

public class ArchiveEventTypeHandler(IApplicationDbContext dbContext) : IRequestHandler<ArchiveEventTypeCommand, Unit>
{
    public async Task<Unit> Handle(ArchiveEventTypeCommand request, CancellationToken cancellationToken)
    {
        var eventType = await dbContext.EventTypes
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken: cancellationToken);

        if (eventType == null) throw new NotFoundException("Event", request.Id);
        if (eventType.IsArchived) throw new UnprocessableEntityException("Event already archived");

        eventType.IsArchived = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}