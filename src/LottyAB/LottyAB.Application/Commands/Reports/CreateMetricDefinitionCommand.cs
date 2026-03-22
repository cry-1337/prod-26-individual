using LottyAB.Contracts.Request.Reports;
using MediatR;

namespace LottyAB.Application.Commands.Reports;

public record CreateMetricDefinitionCommand(CreateMetricDefinitionRequest Request) : IRequest<Guid>;