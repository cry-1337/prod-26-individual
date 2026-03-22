using LottyAB.Application.Commands.Events;
using LottyAB.Application.Queries.Events;
using LottyAB.Contracts.Request.Events;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LottyAB.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> SendEvents([FromBody] SendEventsRequest request)
        => Ok(await mediator.Send(new SendEventsCommand(request)));

    [HttpPost("types")]
    [Authorize("ADMIN")]
    public async Task<IActionResult> CreateEventType([FromBody] CreateEventTypeRequest request)
        => Created(string.Empty, await mediator.Send(request.Adapt<CreateEventTypeCommand>()));

    [HttpGet("types")]
    [Authorize("ADMIN")]
    public async Task<IActionResult> GetEventTypes()
        => Ok(await mediator.Send(new GetEventTypesQuery()));

    [HttpGet("types/{id:guid}")]
    [Authorize("ADMIN")]
    public async Task<IActionResult> GetEventType(Guid id)
        => Ok(await mediator.Send(new GetEventTypeQuery(id)));

    [HttpDelete("types/{id:guid}")]
    [Authorize("ADMIN")]
    public async Task<IActionResult> ArchiveEventType(Guid id)
    {
        await mediator.Send(new ArchiveEventTypeCommand(id));
        return NoContent();
    }

    [HttpGet("attribution/stats")]
    [Authorize("VIEWER")]
    public async Task<IActionResult> GetAttributionStats()
        => Ok(await mediator.Send(new GetAttributionStatsQuery()));
}