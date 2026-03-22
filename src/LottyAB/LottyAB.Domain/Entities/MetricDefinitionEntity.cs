namespace LottyAB.Domain.Entities;

public class MetricDefinitionEntity : BaseEntity
{
    public string MetricKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AggregationType { get; set; } = string.Empty;
    public string? EventTypeKeys { get; set; }
    public bool IsArchived { get; set; } = false;
}