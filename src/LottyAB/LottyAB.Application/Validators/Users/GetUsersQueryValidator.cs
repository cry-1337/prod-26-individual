using FluentValidation;
using LottyAB.Application.Queries.Users;

namespace LottyAB.Application.Validators.Users;

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("PageNumber must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("PageSize must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("PageSize must not exceed 100");

        When(x => x.Role.HasValue, () =>
        {
            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("Invalid user role");
        });
    }
}