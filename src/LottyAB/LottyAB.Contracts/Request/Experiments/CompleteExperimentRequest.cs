using LottyAB.Domain.Enums;

namespace LottyAB.Contracts.Request.Experiments;

public record CompleteExperimentRequest(
    ECompletionOutcome Outcome,
    string Comment,
    Guid? WinnerVariantId = null);