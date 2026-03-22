using FluentValidation;
using LottyAB.Application.Commands.Experiments;

namespace LottyAB.Application.Validators.Experiments;

public class UpdateExperimentCommandValidator : AbstractValidator<UpdateExperimentCommand>
{
    public UpdateExperimentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Experiment ID is required");

        When(x => x.Name != null, () =>
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Experiment name cannot be empty")
                .MaximumLength(255).WithMessage("Experiment name must not exceed 255 characters");
        });

        When(x => x.AudienceFraction.HasValue, () =>
        {
            RuleFor(x => x.AudienceFraction!.Value)
                .GreaterThan(0).WithMessage("Audience fraction must be greater than 0")
                .LessThanOrEqualTo(100).WithMessage("Audience fraction must not exceed 100");
        });

        When(x => x.Variants != null, () =>
        {
            RuleFor(x => x.Variants!)
                .NotEmpty().WithMessage("At least one variant is required")
                .Must(variants => variants!.Count >= 2).WithMessage("At least two variants are required for A/B testing");
        });
    }
}