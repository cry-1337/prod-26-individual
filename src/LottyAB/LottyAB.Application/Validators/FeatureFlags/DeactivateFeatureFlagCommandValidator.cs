using FluentValidation;
using LottyAB.Application.Commands.FeatureFlags;

namespace LottyAB.Application.Validators.FeatureFlags;

public class DeactivateFeatureFlagCommandValidator : AbstractValidator<DeactivateFeatureFlagCommand>
{
    public DeactivateFeatureFlagCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Feature flag ID is required");
    }
}