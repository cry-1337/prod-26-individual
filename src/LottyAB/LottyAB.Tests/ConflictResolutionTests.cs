using LottyAB.Application.Commands;
using LottyAB.Contracts.Request.Experiments;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using LottyAB.Tests.Common;

using ReviewRequest = LottyAB.Contracts.Request.Experiments.ReviewExperimentRequest;

namespace LottyAB.Tests;

public class ConflictResolutionTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    private async Task<(string FlagKey, Guid ExperimentId)> CreateRunningExperimentInDomain(
        string conflictDomains,
        EConflictPolicy conflictPolicy = EConflictPolicy.MutualExclusion,
        int priority = 0)
    {
        var flagKey = $"test-{Guid.NewGuid()}";
        var featureFlagId = await CreateFeatureFlag(flagKey);

        var experimentRequest = new CreateExperimentRequest(
            Name: $"Conflict Experiment {Guid.NewGuid()}",
            Description: null,
            FeatureFlagId: featureFlagId,
            AudienceFraction: 1.0,
            TargetingRule: null,
            PrimaryMetricKey: null,
            Variants:
            [
                new("Control", "control", 0.5, true),
                new("Treatment", "treatment", 0.5, false)
            ],
            ApproverGroupId: null,
            ConflictDomains: conflictDomains,
            ConflictPolicy: conflictPolicy,
            Priority: priority);

        var expResponse = await Client.PostAsJsonAsync("/api/experiments", experimentRequest);
        expResponse.EnsureSuccessStatusCode();
        var experiment = await expResponse.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        var experimentId = experiment!.Id;

        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);
        var reviewRequest = new ReviewRequest(EReviewDecision.Approved);
        await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/review", reviewRequest);
        await Client.PostAsync($"/api/experiments/{experimentId}/start", null);

        return (flagKey, experimentId);
    }

    private async Task<DecisionResponse> Decide(string flagKey, string subjectId)
    {
        var response = await Client.PostAsJsonAsync("/api/decide", new DecideCommand(flagKey, subjectId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DecisionResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task MutualExclusion_NoCompetitors_AlwaysWins()
    {
        await AuthorizeAsAdmin();

        var domain = $"solo-{Guid.NewGuid()}";
        var (flagKey, _) = await CreateRunningExperimentInDomain(domain);

        var decision = await Decide(flagKey, "subject-solo");

        Assert.False(decision.IsDefault);
    }

    [Fact]
    public async Task MutualExclusion_HigherPriority_WinnerGetsVariant()
    {
        await AuthorizeAsAdmin();

        var domain = $"prio-{Guid.NewGuid()}";
        var (flagKeyHigh, _) = await CreateRunningExperimentInDomain(domain, priority: 10);
        var (_, _) = await CreateRunningExperimentInDomain(domain, priority: 5);

        var decision = await Decide(flagKeyHigh, "priority-winner-subject");

        Assert.False(decision.IsDefault);
    }

    [Fact]
    public async Task MutualExclusion_LowerPriority_AlwaysLoses()
    {
        await AuthorizeAsAdmin();

        var domain = $"loser-{Guid.NewGuid()}";
        var (_, _) = await CreateRunningExperimentInDomain(domain, priority: 10);
        var (flagKeyLow, _) = await CreateRunningExperimentInDomain(domain, priority: 5);

        var decision = await Decide(flagKeyLow, "priority-loser-subject");

        Assert.True(decision.IsDefault);
    }

    [Fact]
    public async Task MutualExclusion_EqualPriority_WinnerIsDeterministic()
    {
        await AuthorizeAsAdmin();

        var domain = $"det-{Guid.NewGuid()}";
        var (flagKeyA, _) = await CreateRunningExperimentInDomain(domain, priority: 0);
        var (_, _) = await CreateRunningExperimentInDomain(domain, priority: 0);

        var decision1 = await Decide(flagKeyA, "deterministic-subject");
        var decision2 = await Decide(flagKeyA, "deterministic-subject");

        Assert.Equal(decision1.IsDefault, decision2.IsDefault);
    }

    [Fact]
    public async Task MutualExclusion_EqualPriority_ExactlyOneWins()
    {
        await AuthorizeAsAdmin();

        var domain = $"xor-{Guid.NewGuid()}";
        var (flagKeyA, _) = await CreateRunningExperimentInDomain(domain, priority: 0);
        var (flagKeyB, _) = await CreateRunningExperimentInDomain(domain, priority: 0);

        var decisionA = await Decide(flagKeyA, "xor-subject");
        var decisionB = await Decide(flagKeyB, "xor-subject");

        Assert.True(decisionA.IsDefault ^ decisionB.IsDefault,
            $"Expected exactly one to win: A.IsDefault={decisionA.IsDefault}, B.IsDefault={decisionB.IsDefault}");
    }

    [Fact]
    public async Task MutualExclusion_DifferentDomains_NoConflict()
    {
        await AuthorizeAsAdmin();

        var domainA = $"checkout-{Guid.NewGuid()}";
        var domainB = $"payment-{Guid.NewGuid()}";
        var (flagKeyA, _) = await CreateRunningExperimentInDomain(domainA);
        var (flagKeyB, _) = await CreateRunningExperimentInDomain(domainB);

        var decisionA = await Decide(flagKeyA, "multi-domain-subject");
        var decisionB = await Decide(flagKeyB, "multi-domain-subject");

        Assert.False(decisionA.IsDefault);
        Assert.False(decisionB.IsDefault);
    }

    [Fact]
    public async Task MutualExclusion_NoPolicySet_NoConflict()
    {
        await AuthorizeAsAdmin();

        var domain = $"nopolicy-{Guid.NewGuid()}";
        var (flagKeyNoPolicy, _) = await CreateRunningExperimentWithFlag();
        var (_, _) = await CreateRunningExperimentInDomain(domain);

        var decision = await Decide(flagKeyNoPolicy, "no-policy-subject");

        Assert.False(decision.IsDefault);
    }

    [Fact]
    public async Task MutualExclusion_MultipleDomains_ConflictOnSharedDomain()
    {
        await AuthorizeAsAdmin();

        var sharedDomain = $"shared-{Guid.NewGuid()}";
        var exclusiveDomain = $"exclusive-{Guid.NewGuid()}";

        var (_, _) = await CreateRunningExperimentInDomain($"{sharedDomain},{exclusiveDomain}", priority: 10);
        var (flagKeyB, _) = await CreateRunningExperimentInDomain(sharedDomain, priority: 0);

        var decision = await Decide(flagKeyB, "multi-shared-subject");

        Assert.True(decision.IsDefault);
    }
}