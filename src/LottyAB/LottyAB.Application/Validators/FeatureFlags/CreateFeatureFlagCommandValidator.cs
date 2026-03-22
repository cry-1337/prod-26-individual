using FluentValidation;
using LottyAB.Application.Commands.FeatureFlags;

namespace LottyAB.Application.Validators.FeatureFlags;

public class CreateFeatureFlagCommandValidator : AbstractValidator<CreateFeatureFlagCommand>
{
    public CreateFeatureFlagCommandValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty().WithMessage("Key is required")
            .MaximumLength(255).WithMessage("Key must not exceed 255 characters")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Key must contain only alphanumeric characters, underscores, and hyphens");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters")
            .When(x => x.Description != null);

        RuleFor(x => x.DefaultValue)
            .NotEmpty().WithMessage("DefaultValue is required")
            .MaximumLength(255).WithMessage("DefaultValue must not exceed 255 characters");
    }
}