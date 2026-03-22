using LottyAB.Contracts.Responses.Reports;
using MediatR;

namespace LottyAB.Application.Queries.Reports;

public record GetExperimentReportQuery(
    Guid ExperimentId,
    DateTime? StartDate,
    DateTime? EndDate) : IRequest<ExperimentReportResponse>;