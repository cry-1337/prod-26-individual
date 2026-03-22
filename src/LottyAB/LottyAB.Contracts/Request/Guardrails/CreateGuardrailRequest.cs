using LottyAB.Domain.Enums;

namespace LottyAB.Contracts.Request.Guardrails;

public record CreateGuardrailRequest(
    string MetricKey,
    double Threshold,
    int ObservationWindowMinutes,
    EGuardrailAction Action);