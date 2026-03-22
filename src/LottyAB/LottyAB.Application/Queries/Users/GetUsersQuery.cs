using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;

namespace LottyAB.Application.Queries.Users;

public record GetUsersQuery(
    int PageNumber,
    int PageSize,
    EUserRole? Role,
    bool? IsActive) : IRequest<PagedResponse<UserEntity>>;