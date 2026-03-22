namespace LottyAB.Contracts.Request.Reports;

public record CreateMetricDefinitionRequest(
    string MetricKey,
    string DisplayName,
    string? Description,
    string AggregationType,
    string? EventTypeKeys);