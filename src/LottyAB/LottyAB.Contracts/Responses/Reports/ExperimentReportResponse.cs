namespace LottyAB.Contracts.Responses.Reports;

public record ExperimentReportResponse(
    Guid ExperimentId,
    string ExperimentName,
    DateTime StartDate,
    DateTime EndDate,
    List<VariantMetrics> Variants);

public record VariantMetrics(
    Guid VariantId,
    string VariantName,
    bool IsControl,
    Dictionary<string, double> Metrics,
    Dictionary<string, int> EventCounts);