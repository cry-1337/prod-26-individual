using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Commands.Experiments;

public record RampExperimentCommand(
    Guid ExperimentId,
    double NewAudienceFraction,
    Guid UserId) : IRequest<ExperimentEntity>;