using System.Security.Claims;
using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Queries.Experiments;
using LottyAB.Contracts.Request.Experiments;
using LottyAB.Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LottyAB.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/experiments")]
public class ExperimentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> CreateExperiment([FromBody] CreateExperimentRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new CreateExperimentCommand(
            request.Name,
            request.Description,
            request.FeatureFlagId,
            request.AudienceFraction,
            request.TargetingRule,
            request.PrimaryMetricKey,
            userId,
            request.Variants.Select(v => new VariantCommand(v.Name, v.Value, v.Weight, v.IsControl)).ToList(),
            request.ApproverGroupId,
            request.ConflictDomains,
            request.ConflictPolicy,
            request.Priority);

        var experiment = await mediator.Send(command);
        return Created($"/api/experiments/{experiment.Id}", experiment);
    }

    [HttpGet]
    [Authorize("VIEWER")]
    public async Task<IActionResult> GetExperiments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? featureFlagId = null,
        [FromQuery] EExperimentStatus? status = null)
        => Ok(await mediator.Send(new GetExperimentsQuery(pageNumber, pageSize, status, featureFlagId)));

    [HttpGet("{id:guid}")]
    [Authorize("VIEWER")]
    public async Task<IActionResult> GetExperimentById(Guid id)
        => Ok(await mediator.Send(new GetExperimentByIdQuery(id)));

    [HttpGet("{id:guid}/versions")]
    [Authorize("VIEWER")]
    public async Task<IActionResult> GetExperimentVersions(Guid id)
        => Ok(await mediator.Send(new GetExperimentVersionsQuery(id)));

    [HttpPut("{id:guid}")]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> UpdateExperiment(Guid id, [FromBody] UpdateExperimentRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var variants = request.Variants?.Select(v => new VariantCommand(v.Name, v.Value, v.Weight, v.IsControl)).ToList();
        var command = new UpdateExperimentCommand(
            id,
            userId,
            request.Name,
            request.Description,
            request.AudienceFraction,
            request.TargetingRule,
            request.PrimaryMetricKey,
            variants,
            request.ApproverGroupId,
            request.ConflictDomains,
            request.ConflictPolicy,
            request.Priority);

        var experiment = await mediator.Send(command);
        return Ok(experiment);
    }

    [HttpDelete("{id:guid}")]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> DeleteExperiment(Guid id)
    {
        await mediator.Send(new DeleteExperimentCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/submit")]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> SubmitForReview(Guid id)
        => Ok(await mediator.Send(new SubmitForReviewCommand(id)));

    [HttpPost("{id:guid}/review")]
    [Authorize("APPROVER")]
    public async Task<IActionResult> ReviewExperiment(Guid id, [FromBody] ReviewExperimentRequest request)
    {
        var reviewerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new ReviewExperimentCommand(id, reviewerId, request.Decision, request.Comment);
        var experiment = await mediator.Send(command);
        return Ok(experiment);
    }

    [HttpPost("{id:guid}/start")]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> StartExperiment(Guid id)
        => Ok(await mediator.Send(new StartExperimentCommand(id)));

    [HttpPost("{id:guid}/pause")]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> PauseExperiment(Guid id)
        => Ok(await mediator.Send(new PauseExperimentCommand(id)));

    [HttpPost("{id:guid}/resume")]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> ResumeExperiment(Guid id)
        => Ok(await mediator.Send(new ResumeExperimentCommand(id)));

    [HttpPost("{id:guid}/complete")]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> CompleteExperiment(Guid id, [FromBody] CompleteExperimentRequest request)
        => Ok(await mediator.Send(new CompleteExperimentCommand(id, request.Outcome, request.Comment, request.WinnerVariantId)));

    [HttpPost("{id:guid}/ramp")]
    [Authorize("EXPERIMENTER")]
    public async Task<IActionResult> RampExperiment(Guid id, [FromBody] RampExperimentRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await mediator.Send(new RampExperimentCommand(id, request.NewAudienceFraction, userId)));
    }

    [HttpPost("{id:guid}/archive")]
    [Authorize("ADMIN")]
    public async Task<IActionResult> ArchiveExperiment(Guid id)
        => Ok(await mediator.Send(new ArchiveExperimentCommand(id)));
}