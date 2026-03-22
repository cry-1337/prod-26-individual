using FluentValidation;
using LottyAB.Application.Queries.FeatureFlags;

namespace LottyAB.Application.Validators.FeatureFlags;

public class GetFeatureFlagByIdQueryValidator : AbstractValidator<GetFeatureFlagByIdQuery>
{
    public GetFeatureFlagByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Feature flag ID is required");
    }
}