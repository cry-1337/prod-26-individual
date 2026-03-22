using System.Text.Json;
using System.Text.Json.Serialization;
using LottyAB.Domain.Enums;

namespace LottyAB.Domain.Entities;

public class FeatureFlagEntity : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public EFeatureFlagType ValueType { get; set; } = EFeatureFlagType.String;
    public string DefaultValue { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    [JsonIgnore]
    public ICollection<ExperimentEntity> Experiments { get; set; } = new List<ExperimentEntity>();

    public object? GetTypedDefaultValue()
    {
        if (string.IsNullOrWhiteSpace(DefaultValue))
            return null;

        try
        {
            return ValueType switch
            {
                EFeatureFlagType.String => DefaultValue,
                EFeatureFlagType.Number => double.Parse(DefaultValue),
                EFeatureFlagType.Boolean => bool.Parse(DefaultValue),
                EFeatureFlagType.Json => JsonSerializer.Deserialize<object>(DefaultValue),
                _ => DefaultValue
            };
        }
        catch
        {
            return DefaultValue;
        }
    }

    public bool ValidateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            return ValueType switch
            {
                EFeatureFlagType.String => true,
                EFeatureFlagType.Number => double.TryParse(value, out _),
                EFeatureFlagType.Boolean => bool.TryParse(value, out _),
                EFeatureFlagType.Json => IsValidJson(value),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidJson(string value)
    {
        try
        {
            JsonSerializer.Deserialize<object>(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}