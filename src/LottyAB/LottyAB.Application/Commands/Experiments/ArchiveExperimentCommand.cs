using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Commands.Experiments;

public record ArchiveExperimentCommand(Guid ExperimentId) : IRequest<ExperimentEntity>;