using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Users;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Users;

public class GetUsersHandler(IApplicationDbContext dbContext) : IRequestHandler<GetUsersQuery, PagedResponse<UserEntity>>
{
    public async Task<PagedResponse<UserEntity>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsQueryable();

        if (request.Role.HasValue) query = query.Where(u => u.Role == request.Role.Value);
        if (request.IsActive.HasValue) query = query.Where(u => u.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<UserEntity>
        {
            Items = users,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}