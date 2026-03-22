using LottyAB.Application.Commands.Auth;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Contracts.Responses;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Auth;

public class LoginHandler(IApplicationDbContext dbContext, IJwtService jwtService) : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken: cancellationToken);

        if (user == null) throw new NotFoundException("User", request.Email);
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) throw new UnauthorizedException("Password don't match");

        return new LoginResponse
        {
            AccessToken = jwtService.GenerateAccessToken(user),
            UserData = user
        };
    }
}