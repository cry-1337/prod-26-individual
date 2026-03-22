using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Queries.Experiments;

public record GetExperimentVersionsQuery(Guid ExperimentId) : IRequest<List<ExperimentVersionEntity>>;