using LottyAB.Application.Commands.Auth;
using LottyAB.Contracts.Request.Auth;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LottyAB.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
        => Ok(await mediator.Send(request.Adapt<LoginCommand>()));

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        => Created(string.Empty, await mediator.Send(request.Adapt<RegisterCommand>()));
}