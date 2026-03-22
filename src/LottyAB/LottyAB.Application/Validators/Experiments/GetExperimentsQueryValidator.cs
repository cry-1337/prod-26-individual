using FluentValidation;
using LottyAB.Application.Queries.Experiments;

namespace LottyAB.Application.Validators.Experiments;

public class GetExperimentsQueryValidator : AbstractValidator<GetExperimentsQuery>
{
    public GetExperimentsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("PageNumber must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("PageSize must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("PageSize must not exceed 100");
    }
}