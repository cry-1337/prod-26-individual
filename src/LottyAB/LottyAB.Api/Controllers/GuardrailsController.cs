using LottyAB.Application.Commands.Guardrails;
using LottyAB.Application.Queries.Guardrails;
using LottyAB.Contracts.Request.Guardrails;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LottyAB.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/experiments/{experimentId:guid}/guardrails")]
public class GuardrailsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> CreateGuardrail(Guid experimentId, [FromBody] CreateGuardrailRequest request)
    {
        var command = new CreateGuardrailCommand(
            experimentId,
            request.MetricKey,
            request.Threshold,
            request.ObservationWindowMinutes,
            request.Action);

        var guardrail = await mediator.Send(command);
        return Created($"/api/experiments/{experimentId}/guardrails/{guardrail.Id}", guardrail);
    }

    [HttpGet]
    [Authorize("VIEWER")]
    public async Task<IActionResult> GetGuardrails(Guid experimentId)
        => Ok(await mediator.Send(new GetGuardrailsQuery(experimentId)));

    [HttpGet("history")]
    [Authorize("VIEWER")]
    public async Task<IActionResult> GetTriggerHistory(Guid experimentId)
        => Ok(await mediator.Send(new GetGuardrailTriggerHistoryQuery(experimentId)));

    [HttpDelete("{guardrailId:guid}")]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> DeleteGuardrail(Guid guardrailId)
    {
        await mediator.Send(new DeleteGuardrailCommand(guardrailId));
        return NoContent();
    }
}