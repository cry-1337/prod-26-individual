using MediatR;

namespace LottyAB.Application.Commands.Experiments;

public record DeleteExperimentCommand(Guid Id) : IRequest<Unit>;