using FluentValidation;
using LottyAB.Application.Commands.FeatureFlags;
using LottyAB.Domain.Enums;

namespace LottyAB.Application.Validators;

public class CreateFeatureFlagValidator : AbstractValidator<CreateFeatureFlagCommand>
{
    public CreateFeatureFlagValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.DefaultValue)
            .NotEmpty()
            .Must((command, value) => ValidateValueByType(command.ValueType, value))
            .WithMessage("DefaultValue does not match ValueType");
    }

    private static bool ValidateValueByType(EFeatureFlagType type, string value)
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
            System.Text.Json.JsonSerializer.Deserialize<object>(value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}