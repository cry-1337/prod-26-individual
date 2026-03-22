using MediatR;

namespace LottyAB.Application.Commands.Users;

public record DeactivateUserCommand(Guid Id) : IRequest<Unit>;