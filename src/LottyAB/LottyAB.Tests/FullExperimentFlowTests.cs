using LottyAB.Application.Commands;
using LottyAB.Contracts.Request.Events;
using LottyAB.Contracts.Request.Experiments;
using LottyAB.Contracts.Request.FeatureFlags;
using LottyAB.Contracts.Responses;
using LottyAB.Contracts.Responses.Events;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using LottyAB.Tests.Common;

namespace LottyAB.Tests;

public class FullExperimentFlowTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CompleteExperimentWorkflow_FromCreationToReporting()
    {
        await AuthorizeAsAdmin();

        var createFlagRequest = new CreateFeatureFlagRequest(
            Key: "button-color-test",
            Name: "Button Color Test",
            Description: "Testing different button colors for conversion",
            ValueType: EFeatureFlagType.String,
            DefaultValue: "blue"
        );

        var createFlagResponse = await Client.PostAsJsonAsync("/api/feature-flags", createFlagRequest);
        createFlagResponse.EnsureSuccessStatusCode();
        var featureFlag = await createFlagResponse.Content.ReadFromJsonAsync<FeatureFlagEntity>(JsonOptions);
        Assert.NotNull(featureFlag);

        var createExperimentRequest = new CreateExperimentRequest(
            Name: "Button Color Conversion Test",
            Description: "Testing if green buttons convert better than blue buttons",
            FeatureFlagId: featureFlag.Id,
            AudienceFraction: 1.0,
            TargetingRule: null,
            PrimaryMetricKey: "conversion_rate",
            Variants:
            [
                new("Control - Blue", "blue", 0.5, true),
                new("Treatment - Green", "green", 0.5, false)
            ]
        );

        var createExperimentResponse = await Client.PostAsJsonAsync("/api/experiments", createExperimentRequest);
        createExperimentResponse.EnsureSuccessStatusCode();
        var experiment = await createExperimentResponse.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        Assert.NotNull(experiment);

        var submitResponse = await Client.PostAsync($"/api/experiments/{experiment.Id}/submit", null);
        submitResponse.EnsureSuccessStatusCode();

        var reviewRequest = new ReviewExperimentRequest(
            Decision: EReviewDecision.Approved,
            Comment: "Approved for testing"
        );
        var reviewResponse = await Client.PostAsJsonAsync($"/api/experiments/{experiment.Id}/review", reviewRequest);
        reviewResponse.EnsureSuccessStatusCode();

        var startResponse = await Client.PostAsync($"/api/experiments/{experiment.Id}/start", null);
        startResponse.EnsureSuccessStatusCode();

        var testUsers = new[] { "user-001", "user-002", "user-003", "user-004", "user-005" };
        var decisionIds = new List<Guid>();
        var events = new List<SendEventRequest>();

        foreach (var userId in testUsers)
        {
            var decideCommand = new DecideCommand(
                FeatureFlagKey: "button-color-test",
                SubjectId: userId,
                SubjectAttributes: new Dictionary<string, object>
                {
                    ["country"] = "US",
                    ["platform"] = "web"
                }
            );

            var decideResponse = await Client.PostAsJsonAsync("/api/decide", decideCommand);
            decideResponse.EnsureSuccessStatusCode();
            var decision = await decideResponse.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions);
            Assert.NotNull(decision);
            Assert.NotEqual(Guid.Empty, decision.DecisionId);

            decisionIds.Add(decision.DecisionId);

            events.Add(new SendEventRequest(
                EventId: Guid.NewGuid().ToString(),
                EventTypeKey: "exposure",
                DecisionId: decision.DecisionId,
                SubjectId: userId,
                Properties: new Dictionary<string, object> { ["variant"] = decision.VariantKey! },
                EventTimestamp: DateTime.UtcNow
            ));

            if (userId is "user-001" or "user-002" or "user-004")
            {
                events.Add(new SendEventRequest(
                    EventId: Guid.NewGuid().ToString(),
                    EventTypeKey: "conversion",
                    DecisionId: decision.DecisionId,
                    SubjectId: userId,
                    Properties: new Dictionary<string, object> { ["revenue"] = 29.99 },
                    EventTimestamp: DateTime.UtcNow.AddSeconds(5)
                ));
            }
        }

        var sendEventsRequest = new SendEventsRequest(events);
        var sendEventsResponse = await Client.PostAsJsonAsync("/api/events", sendEventsRequest);
        sendEventsResponse.EnsureSuccessStatusCode();

        var eventsResult = await sendEventsResponse.Content.ReadFromJsonAsync<SendEventsResponse>(JsonOptions);
        Assert.NotNull(eventsResult);
        Assert.Equal(events.Count, eventsResult.Accepted);

        var reportResponse = await Client.GetAsync($"/api/reports/experiments/{experiment.Id}");
        reportResponse.EnsureSuccessStatusCode();

        var report = await reportResponse.Content.ReadFromJsonAsync<LottyAB.Contracts.Responses.Reports.ExperimentReportResponse>(JsonOptions);
        Assert.NotNull(report);
        Assert.Equal(experiment.Id, report.ExperimentId);
        Assert.Equal(testUsers.Length, decisionIds.Count);

        var pauseResponse = await Client.PostAsync($"/api/experiments/{experiment.Id}/pause", null);
        pauseResponse.EnsureSuccessStatusCode();

        var completeRequest = new CompleteExperimentRequest(
            Outcome: ECompletionOutcome.RolloutWinner,
            Comment: "Green button performed better"
        );

        var completeResponse = await Client.PostAsJsonAsync($"/api/experiments/{experiment.Id}/complete", completeRequest);
        completeResponse.EnsureSuccessStatusCode();
    }
}