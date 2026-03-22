using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Queries.Users;

public record GetUserByIdQuery(Guid Id) : IRequest<UserEntity>;