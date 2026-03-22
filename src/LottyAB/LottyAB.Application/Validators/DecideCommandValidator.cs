using FluentValidation;
using LottyAB.Application.Commands;

namespace LottyAB.Application.Validators;

public class DecideCommandValidator : AbstractValidator<DecideCommand>
{
    public DecideCommandValidator()
    {
        RuleFor(x => x.FeatureFlagKey)
            .NotEmpty().WithMessage("FeatureFlagKey is required")
            .MaximumLength(255).WithMessage("FeatureFlagKey must not exceed 255 characters");

        RuleFor(x => x.SubjectId)
            .NotEmpty().WithMessage("SubjectId is required")
            .MaximumLength(255).WithMessage("SubjectId must not exceed 255 characters");
    }
}