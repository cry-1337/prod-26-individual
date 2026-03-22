using LottyAB.Application.Commands.Auth;
using LottyAB.Application.Interfaces;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;

namespace LottyAB.Application.Handlers.Auth;

public class RegisterHandler(IApplicationDbContext dbContext, IJwtService jwtService) : IRequestHandler<RegisterCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var userEntity = new UserEntity
        {
            Email = request.Email,
            Name = request.Name,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = EUserRole.Viewer,
            IsActive = false
        };

        await dbContext.Users.AddAsync(userEntity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponse
        {
            AccessToken = jwtService.GenerateAccessToken(userEntity),
            UserData = userEntity
        };
    }
}