using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Queries.Experiments;

public record GetExperimentByIdQuery(Guid Id) : IRequest<ExperimentEntity>;