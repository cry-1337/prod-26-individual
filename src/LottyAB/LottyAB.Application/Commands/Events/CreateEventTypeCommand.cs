using LottyAB.Contracts.Request.Events;
using MediatR;

namespace LottyAB.Application.Commands.Events;

public record CreateEventTypeCommand(CreateEventTypeRequest Request) : IRequest<Guid>;