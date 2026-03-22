using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;

namespace LottyAB.Application.Commands.Autopilot;

public record CreateRampPlanCommand(
    Guid ExperimentId,
    double[] Steps,
    int MinImpressionsPerStep,
    int MinMinutesPerStep,
    ERampSafetyAction SafetyAction) : IRequest<RampPlanEntity>;