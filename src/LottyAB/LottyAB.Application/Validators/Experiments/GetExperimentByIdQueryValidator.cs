using FluentValidation;
using LottyAB.Application.Queries.Experiments;

namespace LottyAB.Application.Validators.Experiments;

public class GetExperimentByIdQueryValidator : AbstractValidator<GetExperimentByIdQuery>
{
    public GetExperimentByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Experiment ID is required");
    }
}