using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using MediatR;

namespace LottyAB.Application.Commands.Experiments;

public record ReviewExperimentCommand(
    Guid ExperimentId,
    Guid ReviewerId,
    EReviewDecision Decision,
    string? Comment) : IRequest<ExperimentEntity>;