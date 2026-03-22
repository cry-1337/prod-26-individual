using LottyAB.Application.Commands.Users;
using LottyAB.Application.Queries.Users;
using LottyAB.Contracts.Request.Users;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LottyAB.Api.Controllers;

[ApiController]
[Authorize("ADMIN")]
[Route("api/users")]
public class UsersController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        => Created(string.Empty, await mediator.Send(request.Adapt<CreateUserCommand>()));

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Domain.Enums.EUserRole? role = null,
        [FromQuery] bool? isActive = null)
        => Ok(await mediator.Send(new GetUsersQuery(pageNumber, pageSize, role, isActive)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
        => Ok(await mediator.Send(new GetUserByIdQuery(id)));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        => Ok(await mediator.Send(request.Adapt<UpdateUserCommand>() with { Id = id }));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        await mediator.Send(new DeactivateUserCommand(id));
        return NoContent();
    }
}