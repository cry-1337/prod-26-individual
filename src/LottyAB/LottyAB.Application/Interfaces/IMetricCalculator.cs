using LottyAB.Domain.Entities;

namespace LottyAB.Application.Interfaces;

public interface IMetricCalculator
{
    Task<List<MetricDefinitionEntity>> LoadMetricDefinitionsAsync(
        List<string> metricKeys, CancellationToken cancellationToken = default);

    Dictionary<string, double> CalculateMetrics(
        List<MetricDefinitionEntity> definitions, Dictionary<string, int> eventCounts);

    List<string> GetMetricKeysFromExperiment(ExperimentEntity experiment);
}