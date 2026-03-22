using FluentValidation;
using LottyAB.Application.Commands.Users;

namespace LottyAB.Application.Validators.Users;

public class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User ID is required");
    }
}