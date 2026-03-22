using LottyAB.Application.Commands.FeatureFlags;
using LottyAB.Application.Queries.FeatureFlags;
using LottyAB.Contracts.Request.FeatureFlags;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LottyAB.Api.Controllers;

[ApiController]
[Authorize("ADMIN")]
[Route("api/feature-flags")]
public class FeatureFlagsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateFeatureFlag([FromBody] CreateFeatureFlagRequest request)
        => Created(string.Empty, await mediator.Send(request.Adapt<CreateFeatureFlagCommand>()));

    [HttpGet]
    public async Task<IActionResult> GetFeatureFlags(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isActive = null)
        => Ok(await mediator.Send(new GetFeatureFlagsQuery(pageNumber, pageSize, isActive)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetFeatureFlagById(Guid id)
        => Ok(await mediator.Send(new GetFeatureFlagByIdQuery(id)));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateFeatureFlag(Guid id, [FromBody] UpdateFeatureFlagRequest request)
        => Ok(await mediator.Send(request.Adapt<UpdateFeatureFlagCommand>() with { Id = id }));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateFeatureFlag(Guid id)
    {
        await mediator.Send(new DeactivateFeatureFlagCommand(id));
        return NoContent();
    }
}