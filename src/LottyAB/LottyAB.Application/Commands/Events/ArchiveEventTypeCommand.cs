using MediatR;

namespace LottyAB.Application.Commands.Events;

public record ArchiveEventTypeCommand(Guid Id) : IRequest<Unit>;