using System.Net;
using LottyAB.Contracts.Request.Experiments;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using LottyAB.Tests.Common;

namespace LottyAB.Tests;

public class ExperimentsTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateExperiment_WithValidData_ReturnsCreated()
    {
        await AuthorizeAsAdmin();

        var featureFlagId = await CreateFeatureFlag($"exp-test-{Guid.NewGuid()}");

        var request = new CreateExperimentRequest(
            Name: "Test Experiment",
            Description: "Test description",
            FeatureFlagId: featureFlagId,
            AudienceFraction: 1.0,
            TargetingRule: null,
            PrimaryMetricKey: "conversion_rate",
            Variants:
            [
                new("Control", "control", 0.5, true),
                new("Treatment", "treatment", 0.5, false)
            ]
        );

        var response = await Client.PostAsJsonAsync("/api/experiments", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateExperiment_WithInvalidWeights_ReturnsUnprocessableEntity()
    {
        await AuthorizeAsAdmin();

        var featureFlagId = await CreateFeatureFlag($"invalid-weights-{Guid.NewGuid()}");

        var request = new CreateExperimentRequest(
            Name: "Invalid Weights Test",
            Description: null,
            FeatureFlagId: featureFlagId,
            AudienceFraction: 1.0,
            TargetingRule: null,
            PrimaryMetricKey: null,
            Variants:
            [
                new("Control", "control", 0.3, true),
                new("Treatment", "treatment", 0.5, false)
            ]
        );

        var response = await Client.PostAsJsonAsync("/api/experiments", request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetExperiments_ReturnsPagedResults()
    {
        await AuthorizeAsAdmin();

        var response = await Client.GetAsync("/api/experiments?pageNumber=1&pageSize=10");

        response.EnsureSuccessStatusCode();
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<ExperimentEntity>>(JsonOptions);
        Assert.NotNull(pagedResponse);
        Assert.NotNull(pagedResponse.Items);
    }

    [Fact]
    public async Task SubmitExperiment_ChangesStatusToInReview()
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateExperiment();

        var response = await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        response.EnsureSuccessStatusCode();
        var experiment = await response.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.NotNull(experiment);
        Assert.Equal(EExperimentStatus.InReview, experiment.Status);
    }

    [Theory]
    [InlineData(EReviewDecision.Approved, EExperimentStatus.Approved)]
    [InlineData(EReviewDecision.Rejected, EExperimentStatus.Rejected)]
    [InlineData(EReviewDecision.ChangesRequested, EExperimentStatus.Draft)]
    public async Task ReviewExperiment_ChangesStatusCorrectly(EReviewDecision decision, EExperimentStatus expectedStatus)
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateExperiment();
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review",
            new ReviewExperimentRequest(decision));

        response.EnsureSuccessStatusCode();
        var experiment = await response.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.NotNull(experiment);
        Assert.Equal(expectedStatus, experiment.Status);
    }

    [Fact]
    public async Task StartExperiment_ChangesStatusToRunning()
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateExperiment();
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        var reviewRequest = new ReviewExperimentRequest(EReviewDecision.Approved);
        await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);

        var response = await Client.PostAsync($"/api/experiments/{experimentId}/start", null);

        response.EnsureSuccessStatusCode();
        var experiment = await response.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.NotNull(experiment);
        Assert.Equal(EExperimentStatus.Running, experiment.Status);
    }

    [Fact]
    public async Task PauseExperiment_ChangesStatusToPaused()
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateExperiment();
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        var reviewRequest = new ReviewExperimentRequest(EReviewDecision.Approved);
        await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);
        await Client.PostAsync($"/api/experiments/{experimentId}/start", null);

        var response = await Client.PostAsync($"/api/experiments/{experimentId}/pause", null);

        response.EnsureSuccessStatusCode();
        var experiment = await response.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.NotNull(experiment);
        Assert.Equal(EExperimentStatus.Paused, experiment.Status);
    }

    [Fact]
    public async Task CompleteExperiment_ChangesStatusToCompleted()
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateExperiment();
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        var reviewRequest = new ReviewExperimentRequest(EReviewDecision.Approved);
        await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);
        await Client.PostAsync($"/api/experiments/{experimentId}/start", null);
        await Client.PostAsync($"/api/experiments/{experimentId}/pause", null);

        var completeRequest = new CompleteExperimentRequest(
            Outcome: ECompletionOutcome.RolloutWinner,
            Comment: "Winner found"
        );

        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/complete", completeRequest);

        response.EnsureSuccessStatusCode();
        var experiment = await response.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.NotNull(experiment);
        Assert.Equal(EExperimentStatus.Completed, experiment.Status);
    }

    [Fact]
    public async Task DeleteExperiment_InDraftStatus_ReturnsNoContent()
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateExperiment();

        var response = await Client.DeleteAsync($"/api/experiments/{experimentId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task StartExperiment_InDraftStatus_ReturnsUnprocessableEntity()
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateExperiment();

        var response = await Client.PostAsync($"/api/experiments/{experimentId}/start", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task StartExperiment_InReviewStatus_ReturnsUnprocessableEntity()
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateExperiment();
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        var response = await Client.PostAsync($"/api/experiments/{experimentId}/start", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ReviewExperiment_InDraftStatus_ReturnsUnprocessableEntity()
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateExperiment();

        var reviewRequest = new ReviewExperimentRequest(EReviewDecision.Approved);
        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ReviewExperiment_AfterApproved_ReturnsUnprocessableEntity()
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateExperiment();
        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);

        var firstReview = new ReviewExperimentRequest(EReviewDecision.Approved);
        await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", firstReview);

        var secondReview = new ReviewExperimentRequest(EReviewDecision.Approved, "Again");
        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", secondReview);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UpdateExperiment_InRunningStatus_ReturnsUnprocessableEntity()
    {
        await AuthorizeAsAdmin();

        var experimentId = await CreateRunningExperiment();

        var updateRequest = new UpdateExperimentRequest(Name: "New Name");
        var response = await Client.PutAsJsonAsync($"/api/experiments/{experimentId}", updateRequest);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateExperiment_DuplicateActiveFlag_ReturnsConflict()
    {
        await AuthorizeAsAdmin();

        var flagKey = $"conflict-flag-{Guid.NewGuid()}";
        await CreateRunningExperiment(featureFlagKey: flagKey);

        var featureFlagId = await GetFeatureFlagIdByKey(flagKey);
        var request = new CreateExperimentRequest(
            Name: "Duplicate Experiment",
            Description: null,
            FeatureFlagId: featureFlagId,
            AudienceFraction: 1.0,
            TargetingRule: null,
            PrimaryMetricKey: null,
            Variants:
            [
                new("Control", "control", 0.5, true),
                new("Treatment", "treatment", 0.5, false)
            ]
        );

        var response = await Client.PostAsJsonAsync("/api/experiments", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetExperiments_WithoutAuth_ReturnsUnauthorized()
    {
        Client.DefaultRequestHeaders.Authorization = null;

        var response = await Client.GetAsync("/api/experiments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Guid> GetFeatureFlagIdByKey(string key)
    {
        var response = await Client.GetAsync("/api/feature-flags?pageNumber=1&pageSize=100");
        var paged = await response.Content.ReadFromJsonAsync<PagedResponse<FeatureFlagEntity>>(JsonOptions);
        return paged!.Items.First(f => f.Key == key).Id;
    }
}