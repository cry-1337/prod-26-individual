using FluentValidation;
using LottyAB.Application.Commands.Experiments;

namespace LottyAB.Application.Validators.Experiments;

public class ReviewExperimentCommandValidator : AbstractValidator<ReviewExperimentCommand>
{
    public ReviewExperimentCommandValidator()
    {
        RuleFor(x => x.ExperimentId)
            .NotEmpty().WithMessage("Experiment ID is required");

        RuleFor(x => x.ReviewerId)
            .NotEmpty().WithMessage("Reviewer ID is required");

        RuleFor(x => x.Decision)
            .IsInEnum().WithMessage("Invalid review decision");
    }
}