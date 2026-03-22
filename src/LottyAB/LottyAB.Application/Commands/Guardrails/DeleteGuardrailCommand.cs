using MediatR;

namespace LottyAB.Application.Commands.Guardrails;

public record DeleteGuardrailCommand(Guid GuardrailId) : IRequest;