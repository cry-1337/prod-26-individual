using LottyAB.Application.Commands;
using LottyAB.Contracts.Responses;
using LottyAB.Tests.Common;

namespace LottyAB.Tests;

public class DecisionTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Decide_WithRunningExperiment_ReturnsVariant()
    {
        await AuthorizeAsAdmin();
        var (flagKey, _) = await CreateRunningExperimentWithFlag();

        var decideCommand = new DecideCommand(
            FeatureFlagKey: flagKey,
            SubjectId: "test-user-1",
            SubjectAttributes: null
        );

        var response = await Client.PostAsJsonAsync("/api/decide", decideCommand);

        response.EnsureSuccessStatusCode();
        var decision = await response.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions);
        Assert.NotNull(decision);
        Assert.NotEqual(Guid.Empty, decision.DecisionId);
        Assert.NotNull(decision.VariantKey);
    }

    [Fact]
    public async Task Decide_SameSubject_ReturnsSameVariant()
    {
        await AuthorizeAsAdmin();
        var (flagKey, _) = await CreateRunningExperimentWithFlag();

        var decideCommand = new DecideCommand(flagKey, "consistent-user");

        var response1 = await Client.PostAsJsonAsync("/api/decide", decideCommand);
        var decision1 = await response1.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions);

        var response2 = await Client.PostAsJsonAsync("/api/decide", decideCommand);
        var decision2 = await response2.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions);

        Assert.NotNull(decision1);
        Assert.NotNull(decision2);
        Assert.Equal(decision1.VariantKey, decision2.VariantKey);
    }

    [Fact]
    public async Task Decide_WithPausedExperiment_ReturnsDefault()
    {
        await AuthorizeAsAdmin();
        var (flagKey, experimentId) = await CreateRunningExperimentWithFlag();

        await Client.PostAsync($"/api/experiments/{experimentId}/pause", null);

        var decideCommand = new DecideCommand(flagKey, "paused-test-user");
        var response = await Client.PostAsJsonAsync("/api/decide", decideCommand);

        response.EnsureSuccessStatusCode();
        var decision = await response.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions);
        Assert.NotNull(decision);
        Assert.True(decision.IsDefault);
    }

    [Fact]
    public async Task Decide_WithTargeting_RespectsRules()
    {
        await AuthorizeAsAdmin();
        var (flagKey, _) = await CreateRunningExperimentWithFlag(targetingRule: "country == \"US\"");

        var decideCommandUs = new DecideCommand(
            FeatureFlagKey: flagKey,
            SubjectId: "us-user",
            SubjectAttributes: new Dictionary<string, object> { ["country"] = "US" }
        );

        var responseUs = await Client.PostAsJsonAsync("/api/decide", decideCommandUs);
        responseUs.EnsureSuccessStatusCode();
        var decisionUs = await responseUs.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions);
        Assert.NotNull(decisionUs);
        Assert.False(decisionUs.IsDefault);

        var decideCommandUk = new DecideCommand(
            FeatureFlagKey: flagKey,
            SubjectId: "uk-user",
            SubjectAttributes: new Dictionary<string, object> { ["country"] = "UK" }
        );

        var responseUk = await Client.PostAsJsonAsync("/api/decide", decideCommandUk);
        responseUk.EnsureSuccessStatusCode();
        var decisionUk = await responseUk.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions);
        Assert.NotNull(decisionUk);
        Assert.True(decisionUk.IsDefault);
    }

    [Fact]
    public async Task Decide_WithAudienceFraction_RespectsLimits()
    {
        await AuthorizeAsAdmin();
        var (flagKey, _) = await CreateRunningExperimentWithFlag(
            audienceFraction: 0.1,
            controlWeight: 0.05,
            treatmentWeight: 0.05
        );

        var (defaultCount, variantCount) = await CollectDecisions(flagKey, 100);

        Assert.True(defaultCount > variantCount,
            $"Expected more defaults, got {defaultCount} defaults vs {variantCount} variants");
    }

    [Fact]
    public async Task Decide_With50_50Weights_DistributesEvenly()
    {
        await AuthorizeAsAdmin();
        var (flagKey, _) = await CreateRunningExperimentWithFlag(
            controlWeight: 0.5,
            treatmentWeight: 0.5
        );

        var variantCounts = await CollectVariantDistribution(flagKey, 1000);

        Assert.Equal(2, variantCounts.Count);
        var counts = variantCounts.Values.ToArray();
        Assert.InRange(counts[0], 450, 550);
        Assert.InRange(counts[1], 450, 550);
        Assert.Equal(1000, counts.Sum());
    }

    [Fact]
    public async Task Decide_With70_30Weights_DistributesCorrectly()
    {
        await AuthorizeAsAdmin();
        var (flagKey, _) = await CreateRunningExperimentWithFlag(
            controlWeight: 0.7,
            treatmentWeight: 0.3
        );

        var variantCounts = await CollectVariantDistribution(flagKey, 1000);

        Assert.Equal(2, variantCounts.Count);
        var counts = variantCounts.Values.OrderByDescending(c => c).ToArray();
        Assert.InRange(counts[0], 650, 750);
        Assert.InRange(counts[1], 250, 350);
        Assert.Equal(1000, counts.Sum());
    }

    [Fact]
    public async Task Decide_With80_20Weights_DistributesCorrectly()
    {
        await AuthorizeAsAdmin();
        var (flagKey, _) = await CreateRunningExperimentWithFlag(
            controlWeight: 0.8,
            treatmentWeight: 0.2
        );

        var variantCounts = await CollectVariantDistribution(flagKey, 1000);

        Assert.Equal(2, variantCounts.Count);
        var counts = variantCounts.Values.OrderByDescending(c => c).ToArray();
        Assert.InRange(counts[0], 750, 850);
        Assert.InRange(counts[1], 150, 250);
        Assert.Equal(1000, counts.Sum());
    }

    private async Task<(int defaultCount, int variantCount)> CollectDecisions(string flagKey, int iterations)
    {
        var defaultCount = 0;
        var variantCount = 0;

        for (int i = 0; i < iterations; i++)
        {
            var decideCommand = new DecideCommand(flagKey, $"user-{i}");
            var response = await Client.PostAsJsonAsync("/api/decide", decideCommand);
            var decision = await response.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions);

            if (decision!.IsDefault)
                defaultCount++;
            else
                variantCount++;
        }

        return (defaultCount, variantCount);
    }

    [Fact]
    public async Task Decide_ConcurrentExperimentLimit_ReturnsDefaultAfterThree()
    {
        await AuthorizeAsAdmin();

        var flagKeys = new List<string>();
        for (var i = 0; i < 4; i++)
        {
            var (flagKey, _) = await CreateRunningExperimentWithFlag();
            flagKeys.Add(flagKey);
        }

        var subjectId = $"concurrent-{Guid.NewGuid()}";
        var decisions = new List<DecisionResponse>();

        foreach (var decideCommand in flagKeys.Select(flagKey => new DecideCommand(flagKey, subjectId)))
        {
            var response = await Client.PostAsJsonAsync("/api/decide", decideCommand);
            response.EnsureSuccessStatusCode();
            var decision = await response.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions);
            decisions.Add(decision!);
        }

        Assert.False(decisions[0].IsDefault);
        Assert.False(decisions[1].IsDefault);
        Assert.False(decisions[2].IsDefault);

        Assert.True(decisions[3].IsDefault);
    }

    private async Task<Dictionary<string, int>> CollectVariantDistribution(string flagKey, int iterations)
    {
        var variantCounts = new Dictionary<string, int>();

        for (int i = 0; i < iterations; i++)
        {
            var decideCommand = new DecideCommand(flagKey, $"user-{i}");
            var response = await Client.PostAsJsonAsync("/api/decide", decideCommand);
            var decision = await response.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions);

            if (decision is not { IsDefault: false }) continue;
            variantCounts.TryAdd(decision.VariantKey, 0);

            variantCounts[decision.VariantKey]++;
        }

        return variantCounts;
    }
}