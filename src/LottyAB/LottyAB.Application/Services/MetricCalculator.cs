using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Services;

public class MetricCalculator(IApplicationDbContext dbContext) : IMetricCalculator
{
    public async Task<List<MetricDefinitionEntity>> LoadMetricDefinitionsAsync(
        List<string> metricKeys, CancellationToken cancellationToken = default)
    {
        if (metricKeys.Count == 0)
            return [];

        return await dbContext.MetricDefinitions
            .Where(m => metricKeys.Contains(m.MetricKey) && !m.IsArchived)
            .ToListAsync(cancellationToken);
    }

    public Dictionary<string, double> CalculateMetrics(
        List<MetricDefinitionEntity> definitions, Dictionary<string, int> eventCounts)
    {
        var result = new Dictionary<string, double>();

        foreach (var definition in definitions)
        {
            var value = CalculateSingleMetric(definition, eventCounts);
            result[definition.MetricKey] = value;
        }

        return result;
    }

    public List<string> GetMetricKeysFromExperiment(ExperimentEntity experiment)
    {
        var keys = new List<string>();

        if (!string.IsNullOrWhiteSpace(experiment.PrimaryMetricKey))
            keys.Add(experiment.PrimaryMetricKey);

        if (!string.IsNullOrWhiteSpace(experiment.GuardrailMetricKeys))
            keys.AddRange(experiment.GuardrailMetricKeys.Split(',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

        return keys.Distinct().ToList();
    }

    private static double CalculateSingleMetric(MetricDefinitionEntity definition,
        Dictionary<string, int> eventCounts)
    {
        if (string.IsNullOrWhiteSpace(definition.EventTypeKeys))
            return 0.0;

        var eventKeys = definition.EventTypeKeys
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (eventKeys.Length == 0)
            return 0.0;

        return definition.AggregationType.ToLowerInvariant() switch
        {
            "count" => eventKeys.Sum(key => eventCounts.GetValueOrDefault(key, 0)),
            "rate" => CalculateRate(eventKeys, eventCounts),
            "sum" => eventKeys.Sum(key => eventCounts.GetValueOrDefault(key, 0)),
            "average" => CalculateAverage(eventKeys, eventCounts),
            _ => 0.0
        };
    }

    private static double CalculateRate(string[] eventKeys, Dictionary<string, int> eventCounts)
    {
        if (eventKeys.Length < 2)
            return 0.0;

        var denominator = eventCounts.GetValueOrDefault(eventKeys[0], 0);
        var numerator = eventCounts.GetValueOrDefault(eventKeys[1], 0);

        return denominator > 0 ? (double)numerator / denominator : 0.0;
    }

    private static double CalculateAverage(string[] eventKeys, Dictionary<string, int> eventCounts)
    {
        var counts = eventKeys.Select(key => eventCounts.GetValueOrDefault(key, 0)).ToList();
        return counts.Count > 0 ? counts.Average() : 0.0;
    }
}