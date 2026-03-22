using System.Text.Json;

namespace LottyAB.Domain.Entities;

public class EventEntity : BaseEntity
{
    public string EventId { get; set; } = string.Empty;

    public Guid EventTypeId { get; set; }
    public EventTypeEntity EventType { get; set; } = null!;

    public Guid DecisionId { get; set; }
    public DecisionEntity Decision { get; set; } = null!;

    public string SubjectId { get; set; } = string.Empty;

    public string? PropertiesJson { get; set; }

    public DateTime EventTimestamp { get; set; }

    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public bool IsDuplicate { get; set; } = false;

    public bool IsAttributed { get; set; } = false;

    public string? RejectionReason { get; set; }

    public Dictionary<string, object>? GetProperties()
    {
        if (string.IsNullOrWhiteSpace(PropertiesJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(PropertiesJson);
        }
        catch
        {
            return null;
        }
    }

    public void SetProperties(Dictionary<string, object>? properties)
    {
        if (properties == null || properties.Count == 0)
        {
            PropertiesJson = null;
            return;
        }

        PropertiesJson = JsonSerializer.Serialize(properties);
    }
}