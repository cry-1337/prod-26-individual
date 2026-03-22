using System.Net;
using LottyAB.Application.Interfaces;
using LottyAB.Contracts.Request.Experiments;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using LottyAB.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Tests;

public class RolloutTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    private readonly BaseTestFactory m_Factory = factory;

    private async Task<ExperimentEntity> GetExperimentFromDb(Guid id)
    {
        using var scope = m_Factory.Services.CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<IApplicationDbContext>()
            .Experiments
            .Include(e => e.Variants)
            .FirstAsync(e => e.Id == id);
    }

    private async Task<FeatureFlagEntity> GetFeatureFlagFromDb(string key)
    {
        using var scope = m_Factory.Services.CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<IApplicationDbContext>()
            .FeatureFlags
            .FirstAsync(f => f.Key == key);
    }

    private async Task<List<ExperimentVersionEntity>> GetVersionsFromDb(Guid experimentId)
    {
        using var scope = m_Factory.Services.CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<IApplicationDbContext>()
            .ExperimentVersions
            .Where(v => v.ExperimentId == experimentId)
            .ToListAsync();
    }

    [Fact]
    public async Task Ramp_Increases_AudienceFraction_And_Scales_Weights()
    {
        await AuthorizeAsAdmin();

        var (_, experimentId) = await CreateRunningExperimentWithFlag(
            audienceFraction: 0.1,
            controlWeight: 0.05,
            treatmentWeight: 0.05);

        var response = await Client.PostAsJsonAsync(
            $"/api/experiments/{experimentId}/ramp",
            new RampExperimentRequest(0.5));

        response.EnsureSuccessStatusCode();

        var experiment = await GetExperimentFromDb(experimentId);

        Assert.Equal(0.5, experiment.AudienceFraction, 5);

        var control = experiment.Variants.First(v => v.IsControl);
        var treatment = experiment.Variants.First(v => !v.IsControl);

        Assert.Equal(0.25, control.Weight, 5);
        Assert.Equal(0.25, treatment.Weight, 5);

        Assert.Equal(control.Weight / experiment.AudienceFraction,
                     treatment.Weight / experiment.AudienceFraction, 5);
    }

    [Fact]
    public async Task Ramp_Creates_Version_Snapshot()
    {
        await AuthorizeAsAdmin();

        var (_, experimentId) = await CreateRunningExperimentWithFlag(
            audienceFraction: 0.1,
            controlWeight: 0.05,
            treatmentWeight: 0.05);

        var versionsBefore = await GetVersionsFromDb(experimentId);

        var response = await Client.PostAsJsonAsync(
            $"/api/experiments/{experimentId}/ramp",
            new RampExperimentRequest(0.5));

        response.EnsureSuccessStatusCode();

        var versionsAfter = await GetVersionsFromDb(experimentId);

        Assert.Equal(versionsBefore.Count + 1, versionsAfter.Count);
        Assert.Contains(versionsAfter, v => v.ChangeReason == "Audience fraction ramped up");
    }

    [Fact]
    public async Task Ramp_Fails_If_Experiment_Not_Running()
    {
        await AuthorizeAsAdmin();

        var (_, experimentId) = await CreateRunningExperimentWithFlag(
            audienceFraction: 0.1,
            controlWeight: 0.05,
            treatmentWeight: 0.05);

        await Client.PostAsync($"/api/experiments/{experimentId}/pause", null);

        var response = await Client.PostAsJsonAsync(
            $"/api/experiments/{experimentId}/ramp",
            new RampExperimentRequest(0.5));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Ramp_Fails_If_New_Fraction_Is_Less_Than_Current()
    {
        await AuthorizeAsAdmin();

        var (_, experimentId) = await CreateRunningExperimentWithFlag(
            audienceFraction: 0.1,
            controlWeight: 0.05,
            treatmentWeight: 0.05);

        var response = await Client.PostAsJsonAsync(
            $"/api/experiments/{experimentId}/ramp",
            new RampExperimentRequest(0.05));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Ramp_Fails_If_New_Fraction_Above_One()
    {
        await AuthorizeAsAdmin();

        var (_, experimentId) = await CreateRunningExperimentWithFlag(
            audienceFraction: 0.1,
            controlWeight: 0.05,
            treatmentWeight: 0.05);

        var response = await Client.PostAsJsonAsync(
            $"/api/experiments/{experimentId}/ramp",
            new RampExperimentRequest(1.1));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Complete_RolloutWinner_Updates_FeatureFlag_DefaultValue()
    {
        await AuthorizeAsAdmin();

        var (flagKey, experimentId) = await CreateRunningExperimentWithFlag();

        var getResp = await Client.GetAsync($"/api/experiments/{experimentId}");
        getResp.EnsureSuccessStatusCode();
        var exp = await getResp.Content.ReadFromJsonAsync<ExperimentEntity>(JsonOptions);
        var treatmentVariant = exp!.Variants.First(v => !v.IsControl);

        var completeResp = await Client.PostAsJsonAsync(
            $"/api/experiments/{experimentId}/complete",
            new CompleteExperimentRequest(
                Outcome: ECompletionOutcome.RolloutWinner,
                Comment: "Winner rollout test",
                WinnerVariantId: treatmentVariant.Id));

        completeResp.EnsureSuccessStatusCode();

        var flag = await GetFeatureFlagFromDb(flagKey);
        Assert.Equal(treatmentVariant.Value, flag.DefaultValue);
    }

    [Fact]
    public async Task Complete_RolloutWinner_Without_VariantId_Leaves_DefaultValue_Unchanged()
    {
        await AuthorizeAsAdmin();

        var (flagKey, experimentId) = await CreateRunningExperimentWithFlag();

        var flagBefore = await GetFeatureFlagFromDb(flagKey);
        var originalDefault = flagBefore.DefaultValue;

        var completeResp = await Client.PostAsJsonAsync(
            $"/api/experiments/{experimentId}/complete",
            new CompleteExperimentRequest(
                Outcome: ECompletionOutcome.RolloutWinner,
                Comment: "No winner id"));

        completeResp.EnsureSuccessStatusCode();

        var flag = await GetFeatureFlagFromDb(flagKey);
        Assert.Equal(originalDefault, flag.DefaultValue);
    }

    [Fact]
    public async Task Complete_RolloutWinner_Invalid_VariantId_Throws_NotFoundException()
    {
        await AuthorizeAsAdmin();

        var (_, experimentId) = await CreateRunningExperimentWithFlag();

        var response = await Client.PostAsJsonAsync(
            $"/api/experiments/{experimentId}/complete",
            new CompleteExperimentRequest(
                Outcome: ECompletionOutcome.RolloutWinner,
                Comment: "Invalid variant",
                WinnerVariantId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}