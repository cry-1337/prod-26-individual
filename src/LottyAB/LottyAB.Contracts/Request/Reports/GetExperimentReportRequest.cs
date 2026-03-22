namespace LottyAB.Contracts.Request.Reports;

public record GetExperimentReportRequest(
    Guid ExperimentId,
    DateTime? StartDate,
    DateTime? EndDate);