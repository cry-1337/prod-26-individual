using FluentValidation;
using LottyAB.Application.Commands.Experiments;

namespace LottyAB.Application.Validators.Experiments;

public class CreateExperimentCommandValidator : AbstractValidator<CreateExperimentCommand>
{
    public CreateExperimentCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Experiment name is required")
            .MaximumLength(255).WithMessage("Experiment name must not exceed 255 characters");

        RuleFor(x => x.FeatureFlagId)
            .NotEmpty().WithMessage("Feature flag ID is required");

        RuleFor(x => x.AudienceFraction)
            .GreaterThan(0).WithMessage("Audience fraction must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Audience fraction must not exceed 100");

        RuleFor(x => x.Variants)
            .NotEmpty().WithMessage("At least one variant is required")
            .Must(variants => variants.Count >= 2).WithMessage("At least two variants are required for A/B testing");

        RuleForEach(x => x.Variants).ChildRules(variant =>
        {
            variant.RuleFor(v => v.Name)
                .NotEmpty().WithMessage("Variant name is required");

            variant.RuleFor(v => v.Value)
                .NotEmpty().WithMessage("Variant value is required");

            variant.RuleFor(v => v.Weight)
                .GreaterThanOrEqualTo(0).WithMessage("Variant weight must be non-negative")
                .LessThanOrEqualTo(100).WithMessage("Variant weight must not exceed 100");
        });
    }
}