using LottyAB.Contracts.Responses;
using MediatR;

namespace LottyAB.Application.Commands;

public record DecideCommand(
    string FeatureFlagKey,
    string SubjectId,
    Dictionary<string, object>? SubjectAttributes = null
) : IRequest<DecisionResponse>;