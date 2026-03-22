using LottyAB.Domain.Enums;

namespace LottyAB.Contracts.Request.Autopilot;

public record CreateRampPlanRequest(
    double[] Steps,
    int MinImpressionsPerStep,
    int MinMinutesPerStep,
    ERampSafetyAction SafetyAction);