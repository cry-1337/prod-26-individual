namespace LottyAB.Domain.Entities;

public class EventTypeEntity : BaseEntity
{
    public string EventKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool RequiresExposure { get; set; } = true;
    public bool IsExposureEvent { get; set; } = false;
    public string? RequiredPropertiesJson { get; set; }
    public bool IsArchived { get; set; } = false;

    public ICollection<EventEntity> Events { get; set; } = new List<EventEntity>();
}