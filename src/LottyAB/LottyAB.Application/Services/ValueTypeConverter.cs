using System.Text.Json;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Enums;

namespace LottyAB.Application.Services;

public class ValueTypeConverter : IValueTypeConverter
{
    public object? ConvertValue(string value, EFeatureFlagType type)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return type switch
            {
                EFeatureFlagType.String => value,
                EFeatureFlagType.Number => double.Parse(value),
                EFeatureFlagType.Boolean => bool.Parse(value),
                EFeatureFlagType.Json => JsonSerializer.Deserialize<object>(value),
                _ => value
            };
        }
        catch
        {
            return value;
        }
    }

    public bool ValidateValue(string value, EFeatureFlagType type)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            return type switch
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