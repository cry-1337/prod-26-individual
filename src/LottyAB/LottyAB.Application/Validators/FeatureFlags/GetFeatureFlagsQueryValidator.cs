using FluentValidation;
using LottyAB.Application.Queries.FeatureFlags;

namespace LottyAB.Application.Validators.FeatureFlags;

public class GetFeatureFlagsQueryValidator : AbstractValidator<GetFeatureFlagsQuery>
{
    public GetFeatureFlagsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("PageNumber must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("PageSize must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("PageSize must not exceed 100");
    }
}