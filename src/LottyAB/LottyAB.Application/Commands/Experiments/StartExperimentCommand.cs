using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Commands.Experiments;

public record StartExperimentCommand(Guid ExperimentId) : IRequest<ExperimentEntity>;