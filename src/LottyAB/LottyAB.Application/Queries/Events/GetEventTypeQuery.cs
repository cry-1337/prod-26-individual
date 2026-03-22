using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Queries.Events;

public record GetEventTypeQuery(Guid Id) : IRequest<EventTypeEntity>;