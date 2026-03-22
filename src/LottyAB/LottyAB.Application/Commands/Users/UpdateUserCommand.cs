using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;

namespace LottyAB.Application.Commands.Users;

public record UpdateUserCommand(
    Guid Id,
    string? Name,
    string? Email,
    string? Password,
    EUserRole? Role,
    bool? IsActive) : IRequest<UserEntity>;