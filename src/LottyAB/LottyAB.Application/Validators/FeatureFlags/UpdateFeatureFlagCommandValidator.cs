using FluentValidation;
using LottyAB.Application.Commands.FeatureFlags;

namespace LottyAB.Application.Validators.FeatureFlags;

public class UpdateFeatureFlagCommandValidator : AbstractValidator<UpdateFeatureFlagCommand>
{
    public UpdateFeatureFlagCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Feature flag ID is required");

        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name cannot be empty")
                .MaximumLength(255).WithMessage("Name must not exceed 255 characters");
        });

        When(x => x.Description != null, () =>
        {
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");
        });

        When(x => x.DefaultValue != null, () =>
        {
            RuleFor(x => x.DefaultValue)
                .NotEmpty().WithMessage("DefaultValue cannot be empty")
                .MaximumLength(255).WithMessage("DefaultValue must not exceed 255 characters");
        });
    }
}