using LottyAB.Application.Commands.Autopilot;
using LottyAB.Application.Queries.Autopilot;
using LottyAB.Contracts.Request.Autopilot;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LottyAB.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/experiments/{id:guid}/autopilot")]
public class AutopilotController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> CreateRampPlan(Guid id, [FromBody] CreateRampPlanRequest request)
    {
        var command = new CreateRampPlanCommand(
            id,
            request.Steps,
            request.MinImpressionsPerStep,
            request.MinMinutesPerStep,
            request.SafetyAction);

        var plan = await mediator.Send(command);
        return Created($"/api/experiments/{id}/autopilot", plan);
    }

    [HttpGet]
    [Authorize("VIEWER")]
    public async Task<IActionResult> GetRampPlan(Guid id)
        => Ok(await mediator.Send(new GetRampPlanQuery(id)));

    [HttpGet("history")]
    [Authorize("VIEWER")]
    public async Task<IActionResult> GetRampPlanHistory(Guid id)
        => Ok(await mediator.Send(new GetRampPlanHistoryQuery(id)));

    [HttpPost("enable")]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> EnableRampPlan(Guid id)
        => Ok(await mediator.Send(new SetRampPlanEnabledCommand(id, true)));

    [HttpPost("disable")]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> DisableRampPlan(Guid id)
        => Ok(await mediator.Send(new SetRampPlanEnabledCommand(id, false)));
}