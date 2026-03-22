using FluentValidation;
using LottyAB.Application.Commands.Experiments;

namespace LottyAB.Application.Validators.Experiments;

public class CompleteExperimentCommandValidator : AbstractValidator<CompleteExperimentCommand>
{
    public CompleteExperimentCommandValidator()
    {
        RuleFor(x => x.ExperimentId)
            .NotEmpty().WithMessage("Experiment ID is required");

        RuleFor(x => x.Outcome)
            .IsInEnum().WithMessage("Invalid completion outcome");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Comment is required for experiment completion")
            .MaximumLength(2000).WithMessage("Comment must not exceed 2000 characters");
    }
}