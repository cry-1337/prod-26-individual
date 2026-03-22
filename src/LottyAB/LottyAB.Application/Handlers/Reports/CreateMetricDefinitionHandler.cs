using LottyAB.Application.Commands.Reports;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Reports;

public class CreateMetricDefinitionHandler(IApplicationDbContext dbContext)
    : IRequestHandler<CreateMetricDefinitionCommand, Guid>
{
    public async Task<Guid> Handle(CreateMetricDefinitionCommand request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.MetricDefinitions
            .AnyAsync(m => m.MetricKey == request.Request.MetricKey, cancellationToken);

        if (exists) throw new ConflictException($"Metric definition with key '{request.Request.MetricKey}' already exists");

        var metricDefinition = new MetricDefinitionEntity
        {
            MetricKey = request.Request.MetricKey,
            DisplayName = request.Request.DisplayName,
            Description = request.Request.Description,
            AggregationType = request.Request.AggregationType,
            EventTypeKeys = request.Request.EventTypeKeys,
            IsArchived = false
        };

        dbContext.MetricDefinitions.Add(metricDefinition);
        await dbContext.SaveChangesAsync(cancellationToken);

        return metricDefinition.Id;
    }
}