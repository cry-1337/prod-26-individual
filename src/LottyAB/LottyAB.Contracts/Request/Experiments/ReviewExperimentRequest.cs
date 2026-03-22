using LottyAB.Domain.Enums;

namespace LottyAB.Contracts.Request.Experiments;

public record ReviewExperimentRequest(
    EReviewDecision Decision,
    string? Comment = null);