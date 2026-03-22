using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Application.Queries.Reports;
using LottyAB.Contracts.Responses.Reports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Handlers.Reports;

public class GetExperimentReportHandler(
    IApplicationDbContext dbContext,
    IMetricCalculator metricCalculator) : IRequestHandler<GetExperimentReportQuery, ExperimentReportResponse>
{
    public async Task<ExperimentReportResponse> Handle(GetExperimentReportQuery request,
        CancellationToken cancellationToken)
    {
        var experiment = await dbContext.Experiments
            .Include(e => e.Variants)
            .FirstOrDefaultAsync(e => e.Id == request.ExperimentId, cancellationToken);

        if (experiment == null)
            throw new NotFoundException("Experiment", request.ExperimentId.ToString());

        var startDate = request.StartDate ?? experiment.StartedAt ?? experiment.CreatedAt;
        var endDate = request.EndDate ?? experiment.CompletedAt ?? DateTime.UtcNow;

        var metricKeys = metricCalculator.GetMetricKeysFromExperiment(experiment);
        var metricDefinitions = await metricCalculator.LoadMetricDefinitionsAsync(metricKeys, cancellationToken);

        var variantMetrics = new List<VariantMetrics>();

        foreach (var variant in experiment.Variants)
        {
            var decisions = await dbContext.Decisions
                .Where(d => d.VariantId == variant.Id && d.Timestamp >= startDate && d.Timestamp < endDate)
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);

            if (decisions.Count == 0)
            {
                variantMetrics.Add(new VariantMetrics(
                    variant.Id,
                    variant.Name,
                    variant.IsControl,
                    new Dictionary<string, double>(),
                    new Dictionary<string, int>()
                ));
                continue;
            }

            var events = await dbContext.Events
                .Where(e => decisions.Contains(e.DecisionId) &&
                            e.EventTimestamp >= startDate &&
                            e.EventTimestamp < endDate &&
                            e.IsAttributed)
                .Include(e => e.EventType)
                .ToListAsync(cancellationToken);

            var eventCounts = events
                .GroupBy(e => e.EventType.EventKey)
                .ToDictionary(g => g.Key, g => g.Count());

            var metrics = metricCalculator.CalculateMetrics(metricDefinitions, eventCounts);

            variantMetrics.Add(new VariantMetrics(
                variant.Id,
                variant.Name,
                variant.IsControl,
                metrics,
                eventCounts
            ));
        }

        return new ExperimentReportResponse(
            experiment.Id,
            experiment.Name,
            startDate,
            endDate,
            variantMetrics
        );
    }
}