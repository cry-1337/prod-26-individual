using LottyAB.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LottyAB.Api.Controllers;

[ApiController]
[Route("api")]
public class DecideController(IMediator mediator) : ControllerBase
{
    [HttpPost("decide")]
    public async Task<IActionResult> Decide(DecideCommand decideRequest)
        => Ok(await mediator.Send(decideRequest));
}