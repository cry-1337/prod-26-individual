using LottyAB.Contracts.Responses;
using LottyAB.Domain.Enums;
using MediatR;

namespace LottyAB.Application.Commands.Guardrails;

public record CreateGuardrailCommand(
    Guid ExperimentId,
    string MetricKey,
    double Threshold,
    int ObservationWindowMinutes,
    EGuardrailAction Action) : IRequest<GuardrailResponse>;