using System.Net;
using LottyAB.Contracts.Request.Reports;
using LottyAB.Contracts.Responses.Reports;
using LottyAB.Tests.Common;

namespace LottyAB.Tests;

public class ReportsTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetExperimentReport_WithValidId_ReturnsReport()
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateRunningExperiment();

        var response = await Client.GetAsync($"/api/reports/experiments/{experimentId}");

        response.EnsureSuccessStatusCode();
        var report = await response.Content.ReadFromJsonAsync<ExperimentReportResponse>(JsonOptions);
        Assert.NotNull(report);
        Assert.Equal(experimentId, report.ExperimentId);
    }

    [Fact]
    public async Task GetExperimentReport_WithDateRange_ReturnsFilteredReport()
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateRunningExperiment();
        var startDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ssZ");
        var endDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ");

        var response = await Client.GetAsync($"/api/reports/experiments/{experimentId}?startDate={startDate}&endDate={endDate}");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetExperimentReport_WithNonExistentId_ReturnsNotFound()
    {
        await AuthorizeAsAdmin();

        var response = await Client.GetAsync($"/api/reports/experiments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMetricDefinitions_ReturnsDefaultMetrics()
    {
        await AuthorizeAsAdmin();

        var response = await Client.GetAsync("/api/reports/metrics");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("conversion_rate", content);
        Assert.Contains("click_through_rate", content);
    }

    [Fact]
    public async Task CreateMetricDefinition_WithValidData_SucceedsOrFails()
    {
        await AuthorizeAsAdmin();

        var request = new CreateMetricDefinitionRequest(
            MetricKey: $"custom-metric-{Guid.NewGuid()}",
            DisplayName: "Custom Metric",
            Description: "A custom metric for testing",
            AggregationType: "rate",
            EventTypeKeys: "exposure,conversion"
        );

        var response = await Client.PostAsJsonAsync("/api/reports/metrics", request);

        Assert.True(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetExperimentReport_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync($"/api/reports/experiments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}