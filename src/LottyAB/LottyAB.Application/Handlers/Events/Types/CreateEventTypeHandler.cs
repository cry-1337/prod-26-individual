using LottyAB.Application.Commands.Events;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Events.Types;

public class CreateEventTypeHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateEventTypeCommand, Guid>
{
    public async Task<Guid> Handle(CreateEventTypeCommand request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.EventTypes
            .AnyAsync(et => et.EventKey == request.Request.EventKey, cancellationToken);

        if (exists)
            throw new ConflictException($"Event type with key '{request.Request.EventKey}' already exists");

        var eventType = new EventTypeEntity
        {
            EventKey = request.Request.EventKey,
            DisplayName = request.Request.DisplayName,
            Description = request.Request.Description,
            RequiresExposure = request.Request.RequiresExposure,
            IsExposureEvent = request.Request.IsExposureEvent,
            IsArchived = false
        };

        dbContext.EventTypes.Add(eventType);
        await dbContext.SaveChangesAsync(cancellationToken);

        return eventType.Id;
    }
}