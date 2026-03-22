using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Queries.Events;

public record GetEventTypesQuery : IRequest<List<EventTypeEntity>>;