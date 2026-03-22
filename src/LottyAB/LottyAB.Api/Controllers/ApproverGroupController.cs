using LottyAB.Application.Commands.ApproverGroups;
using LottyAB.Application.Queries;
using LottyAB.Application.Queries.ApproverGroups;
using LottyAB.Contracts.Request.ApproverGroups;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LottyAB.Api.Controllers;

[Authorize("ADMIN")]
[ApiController]
[Route("api/approver-group")]
public class ApproverGroupController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetApproverGroups([FromQuery] int page = 0, [FromQuery] int size = 20)
        => Ok(await mediator.Send(new GetApproverGroupsQuery(page, size)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetApproverGroup(Guid id)
        => Ok(await mediator.Send(new GetApproverGroupQuery(id)));

    [HttpPost]
    public async Task<IActionResult> CreateApproverGroup([FromBody] CreateApproverGroupRequest request)
        => Created(string.Empty, await mediator.Send(new CreateApproverGroupCommand(
            request.Name, request.Description, request.ApproversToStart, request.ApproverIds)));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteApproverGroup(Guid id)
    {
        await mediator.Send(new DeleteApproverGroupCommand(id));
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateApproverGroup(Guid id, [FromBody] UpdateApproverGroupRequest request)
        => Ok(await mediator.Send(new UpdateApproverGroupCommand(
            id, request.Name, request.Description, request.ApproversToStart, request.ApproverIds)));
}