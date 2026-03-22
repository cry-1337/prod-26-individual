using FluentValidation;
using LottyAB.Application.Commands.Experiments;
using LottyAB.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Validators;

public class CreateExperimentValidator : AbstractValidator<CreateExperimentCommand>
{
    public CreateExperimentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.AudienceFraction)
            .GreaterThan(0)
            .LessThanOrEqualTo(1);

        RuleFor(x => x.Variants)
            .NotEmpty()
            .Must(variants => variants.Count >= 2)
            .WithMessage("Experiment must have at least 2 variants");

        RuleFor(x => x.Variants)
            .Must(variants => variants.Count(v => v.IsControl) == 1)
            .WithMessage("Experiment must have exactly one control variant");
    }
}