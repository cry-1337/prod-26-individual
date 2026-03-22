using LottyAB.Application.Commands.Reports;
using LottyAB.Application.Queries.Reports;
using LottyAB.Contracts.Request.Reports;
using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LottyAB.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet("experiments/{experimentId:guid}")]
    [Authorize("VIEWER")]
    public async Task<IActionResult> GetExperimentReport(
        Guid experimentId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
        => Ok(await mediator.Send(new GetExperimentReportQuery(experimentId, startDate, endDate)));

    [HttpPost("metrics")]
    [Authorize("ADMIN")]
    public async Task<IActionResult> CreateMetricDefinition([FromBody] CreateMetricDefinitionRequest request)
        => Created(string.Empty, await mediator.Send(request.Adapt<CreateMetricDefinitionCommand>()));

    [HttpGet("metrics")]
    [Authorize("ADMIN")]
    public async Task<IActionResult> GetMetricDefinitions()
        => Ok(await mediator.Send(new GetMetricDefinitionsQuery()));
}