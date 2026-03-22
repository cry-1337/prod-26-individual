using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;

namespace LottyAB.Application.Commands.Users;

public record CreateUserCommand(
    string Name,
    string Email,
    string Password,
    EUserRole Role) : IRequest<UserEntity>;