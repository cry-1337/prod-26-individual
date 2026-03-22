using System.Net;
using LottyAB.Contracts.Request.Guardrails;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Enums;
using LottyAB.Tests.Common;

namespace LottyAB.Tests;

public class GuardrailTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateGuardrail_WithValidData_ReturnsCreated()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateApprovedExperiment();

        var guardrailRequest = CreateValidGuardrailRequest();

        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/guardrails", guardrailRequest);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var guardrail = await response.Content.ReadFromJsonAsync<GuardrailResponse>(JsonOptions);
        Assert.NotNull(guardrail);
        Assert.NotEqual(Guid.Empty, guardrail.Id);
        Assert.Equal("conversion_rate", guardrail.MetricKey);
        Assert.Equal(0.05, guardrail.Threshold);
        Assert.Equal(60, guardrail.ObservationWindowMinutes);
        Assert.Equal(EGuardrailAction.Pause, guardrail.Action);
        Assert.True(guardrail.IsActive);
    }

    [Fact]
    public async Task GetGuardrails_ForExperiment_ReturnsGuardrailsList()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateApprovedExperiment();

        var guardrailRequest = CreateValidGuardrailRequest(
            metricKey: "error_rate",
            threshold: 0.10,
            observationWindow: 30,
            action: EGuardrailAction.RollbackToControl
        );

        await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/guardrails", guardrailRequest);

        var response = await Client.GetAsync($"/api/experiments/{experimentId}/guardrails");

        response.EnsureSuccessStatusCode();

        var guardrails = await response.Content.ReadFromJsonAsync<List<GuardrailResponse>>(JsonOptions);
        Assert.NotNull(guardrails);
        Assert.NotEmpty(guardrails);

        var firstGuardrail = guardrails[0];
        Assert.Equal("error_rate", firstGuardrail.MetricKey);
        Assert.Equal(0.10, firstGuardrail.Threshold);
    }

    [Fact]
    public async Task GetTriggerHistory_ForExperiment_ReturnsHistory()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateApprovedExperiment();

        var response = await Client.GetAsync($"/api/experiments/{experimentId}/guardrails/history");

        response.EnsureSuccessStatusCode();

        var history = await response.Content.ReadFromJsonAsync<List<GuardrailTriggerHistoryResponse>>(JsonOptions);
        Assert.NotNull(history);
    }

    [Fact]
    public async Task DeleteGuardrail_WithValidId_ReturnsNoContent()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateApprovedExperiment();

        var guardrailRequest = CreateValidGuardrailRequest(
            metricKey: "click_through_rate",
            threshold: 0.20,
            observationWindow: 90
        );

        var createResponse = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/guardrails", guardrailRequest);
        createResponse.EnsureSuccessStatusCode();

        var guardrail = await createResponse.Content.ReadFromJsonAsync<GuardrailResponse>();
        Assert.NotNull(guardrail);

        var deleteResponse = await Client.DeleteAsync($"/api/experiments/{experimentId}/guardrails/{guardrail.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateGuardrail_WithNegativeThreshold_ReturnsBadRequest()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateApprovedExperiment();

        var guardrailRequest = CreateValidGuardrailRequest(threshold: -0.05);

        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/guardrails", guardrailRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGuardrail_WithInvalidMetricKey_ReturnsBadRequest()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateApprovedExperiment();

        var guardrailRequest = CreateValidGuardrailRequest(metricKey: "");

        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/guardrails", guardrailRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGuardrail_WithZeroObservationWindow_ReturnsBadRequest()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateApprovedExperiment();

        var guardrailRequest = CreateValidGuardrailRequest(observationWindow: 0);

        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/guardrails", guardrailRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGuardrail_WithNonExistentExperiment_ReturnsNotFound()
    {
        await AuthorizeAsAdmin();

        var guardrailRequest = CreateValidGuardrailRequest();

        var response = await Client.PostAsJsonAsync($"/api/experiments/{Guid.NewGuid()}/guardrails", guardrailRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateGuardrail_WithNonExistentMetric_ReturnsNotFound()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateApprovedExperiment();

        var guardrailRequest = CreateValidGuardrailRequest(metricKey: "non_existent_metric");

        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/guardrails", guardrailRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static CreateGuardrailRequest CreateValidGuardrailRequest(
        string metricKey = "conversion_rate",
        double threshold = 0.05,
        int observationWindow = 60,
        EGuardrailAction action = EGuardrailAction.Pause)
        => new(metricKey, threshold, observationWindow, action);
}