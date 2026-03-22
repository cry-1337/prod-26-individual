using LottyAB.Application.Commands.Users;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Users;

public class DeactivateUserHandler(IApplicationDbContext dbContext) : IRequestHandler<DeactivateUserCommand, Unit>
{
    public async Task<Unit> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user == null) throw new NotFoundException($"User with ID {request.Id} not found");

        user.IsActive = false;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}