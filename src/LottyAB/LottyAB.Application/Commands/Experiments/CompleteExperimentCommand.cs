using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;

namespace LottyAB.Application.Commands.Experiments;

public record CompleteExperimentCommand(
    Guid ExperimentId,
    ECompletionOutcome Outcome,
    string Comment,
    Guid? WinnerVariantId = null) : IRequest<ExperimentEntity>;