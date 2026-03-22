using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Commands.Experiments;

public record ResumeExperimentCommand(Guid ExperimentId) : IRequest<ExperimentEntity>;