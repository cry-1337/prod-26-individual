using LottyAB.Domain.Entities;
using MediatR;

namespace LottyAB.Application.Queries.Reports;

public record GetMetricDefinitionsQuery : IRequest<List<MetricDefinitionEntity>>;