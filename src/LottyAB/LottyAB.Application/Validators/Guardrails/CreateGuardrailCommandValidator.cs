using FluentValidation;
using LottyAB.Application.Commands.Guardrails;

namespace LottyAB.Application.Validators.Guardrails;

public class CreateGuardrailCommandValidator : AbstractValidator<CreateGuardrailCommand>
{
    public CreateGuardrailCommandValidator()
    {
        RuleFor(x => x.MetricKey)
            .NotEmpty()
            .WithMessage("Metric key is required");

        RuleFor(x => x.Threshold)
            .GreaterThan(0)
            .WithMessage("Threshold must be greater than 0");

        RuleFor(x => x.ObservationWindowMinutes)
            .GreaterThan(0)
            .WithMessage("Observation window must be greater than 0");
    }
}