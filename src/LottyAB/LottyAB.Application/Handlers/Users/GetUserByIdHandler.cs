using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Users;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Users;

public class GetUserByIdHandler(IApplicationDbContext dbContext) : IRequestHandler<GetUserByIdQuery, UserEntity>
{
    public async Task<UserEntity> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        return user ?? throw new NotFoundException($"User with ID {request.Id} not found");
    }
}