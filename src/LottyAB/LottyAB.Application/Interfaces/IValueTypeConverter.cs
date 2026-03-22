using LottyAB.Domain.Enums;

namespace LottyAB.Application.Interfaces;

public interface IValueTypeConverter
{
    object? ConvertValue(string value, EFeatureFlagType type);
    bool ValidateValue(string value, EFeatureFlagType type);
}