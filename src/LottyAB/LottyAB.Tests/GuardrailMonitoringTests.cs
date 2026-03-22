using LottyAB.Application.Interfaces;
using LottyAB.Contracts.Request.Guardrails;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using LottyAB.Infrastructure.Services;
using LottyAB.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ReviewRequest = LottyAB.Contracts.Request.Experiments.ReviewExperimentRequest;

namespace LottyAB.Tests;

public class GuardrailMonitoringTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    private readonly BaseTestFactory m_Factory = factory;

    private GuardrailMonitoringService CreateMonitoringService() =>
        new(m_Factory.Services,
            m_Factory.Services.GetRequiredService<ILogger<GuardrailMonitoringService>>());

    private async Task<(Guid ExperimentId, Guid GuardrailId)> SetupRunningExperimentWithGuardrail(
        double threshold = 0.5,
        EGuardrailAction action = EGuardrailAction.Pause)
    {
        var experimentId = await CreateExperiment(primaryMetricKey: "error_rate");

        var guardrailRequest = new CreateGuardrailRequest("error_rate", threshold, 60, action);
        var guardrailResp = await Client.PostAsJsonAsync(
            $"/api/experiments/{experimentId}/guardrails", guardrailRequest);
        guardrailResp.EnsureSuccessStatusCode();
        var guardrail = await guardrailResp.Content.ReadFromJsonAsync<GuardrailResponse>(JsonOptions);

        await Client.PostAsync($"/api/experiments/{experimentId}/submit", null);
        await Client.PostAsJsonAsync(
            $"/api/experiments/{experimentId}/review",
            new ReviewRequest(EReviewDecision.Approved));
        await Client.PostAsync($"/api/experiments/{experimentId}/start", null);

        return (experimentId, guardrail!.Id);
    }

    private async Task InsertDecisionsAndEvents(
        Guid experimentId,
        int exposureCount,
        int errorCount)
    {
        using var scope = m_Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var experiment = await db.Experiments.FirstAsync(e => e.Id == experimentId);
        var exposureType = await db.EventTypes.FirstAsync(et => et.EventKey == "exposure");
        var errorType = await db.EventTypes.FirstAsync(et => et.EventKey == "error");

        var decisions = Enumerable.Range(0, exposureCount).Select(_ => new DecisionEntity
        {
            ExperimentId = experimentId,
            FeatureFlagId = experiment.FeatureFlagId,
            SubjectId = Guid.NewGuid().ToString(),
            VariantValue = "treatment",
            IsDefault = false,
            Timestamp = DateTime.UtcNow
        }).ToList();

        db.Decisions.AddRange(decisions);
        await db.SaveChangesAsync();

        db.Events.AddRange(decisions.Select(d => new EventEntity
        {
            EventId = Guid.NewGuid().ToString(),
            DecisionId = d.Id,
            EventTypeId = exposureType.Id,
            SubjectId = d.SubjectId,
            IsAttributed = true,
            EventTimestamp = DateTime.UtcNow
        }));

        db.Events.AddRange(decisions.Take(errorCount).Select(d => new EventEntity
        {
            EventId = Guid.NewGuid().ToString(),
            DecisionId = d.Id,
            EventTypeId = errorType.Id,
            SubjectId = d.SubjectId,
            IsAttributed = true,
            EventTimestamp = DateTime.UtcNow
        }));

        await db.SaveChangesAsync();
    }

    private async Task<ExperimentEntity> GetExperiment(Guid id)
    {
        using var scope = m_Factory.Services.CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<IApplicationDbContext>()
            .Experiments.FirstAsync(e => e.Id == id);
    }

    private async Task<List<GuardrailTriggerHistoryEntity>> GetTriggerHistory(Guid experimentId)
    {
        using var scope = m_Factory.Services.CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<IApplicationDbContext>()
            .GuardrailTriggerHistory
            .Where(h => h.ExperimentId == experimentId)
            .ToListAsync();
    }

    private async Task<GuardrailEntity> GetGuardrail(Guid id)
    {
        using var scope = m_Factory.Services.CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<IApplicationDbContext>()
            .Guardrails.FirstAsync(g => g.Id == id);
    }

    [Fact]
    public async Task CheckGuardrails_WhenErrorRateExceedsThreshold_PausesExperiment()
    {
        await AuthorizeAsAdmin();
        var (experimentId, _) = await SetupRunningExperimentWithGuardrail(
            threshold: 0.5, action: EGuardrailAction.Pause);
        await InsertDecisionsAndEvents(experimentId, exposureCount: 10, errorCount: 8);

        await CreateMonitoringService().CheckGuardrails(CancellationToken.None);

        var experiment = await GetExperiment(experimentId);
        Assert.Equal(EExperimentStatus.Paused, experiment.Status);
    }

    [Fact]
    public async Task CheckGuardrails_WhenErrorRateExceedsThreshold_RollsBackToControl()
    {
        await AuthorizeAsAdmin();
        var (experimentId, _) = await SetupRunningExperimentWithGuardrail(
            threshold: 0.5, action: EGuardrailAction.RollbackToControl);
        await InsertDecisionsAndEvents(experimentId, exposureCount: 10, errorCount: 8);

        await CreateMonitoringService().CheckGuardrails(CancellationToken.None);

        var experiment = await GetExperiment(experimentId);
        Assert.Equal(EExperimentStatus.Completed, experiment.Status);
        Assert.Equal(ECompletionOutcome.Rollback, experiment.Outcome);
    }

    [Fact]
    public async Task CheckGuardrails_WhenMetricBelowThreshold_DoesNotChangeStatus()
    {
        await AuthorizeAsAdmin();
        var (experimentId, _) = await SetupRunningExperimentWithGuardrail(threshold: 0.5);
        await InsertDecisionsAndEvents(experimentId, exposureCount: 10, errorCount: 2);

        await CreateMonitoringService().CheckGuardrails(CancellationToken.None);

        var experiment = await GetExperiment(experimentId);
        Assert.Equal(EExperimentStatus.Running, experiment.Status);
    }

    [Fact]
    public async Task CheckGuardrails_WhenTriggered_CreatesTriggerHistoryRecord()
    {
        await AuthorizeAsAdmin();
        var (experimentId, _) = await SetupRunningExperimentWithGuardrail(
            threshold: 0.5, action: EGuardrailAction.Pause);
        await InsertDecisionsAndEvents(experimentId, exposureCount: 10, errorCount: 8);

        await CreateMonitoringService().CheckGuardrails(CancellationToken.None);

        var history = await GetTriggerHistory(experimentId);
        Assert.Single(history);
        var record = history[0];
        Assert.Equal("error_rate", record.MetricKey);
        Assert.Equal(0.5, record.Threshold);
        Assert.Equal(0.8, record.ActualValue, 5);
        Assert.Equal(EGuardrailAction.Pause, record.ActionTaken);
    }

    [Fact]
    public async Task CheckGuardrails_WhenTriggered_DeactivatesGuardrail()
    {
        await AuthorizeAsAdmin();
        var (experimentId, guardrailId) = await SetupRunningExperimentWithGuardrail(threshold: 0.5);
        await InsertDecisionsAndEvents(experimentId, exposureCount: 10, errorCount: 8);

        await CreateMonitoringService().CheckGuardrails(CancellationToken.None);

        var guardrail = await GetGuardrail(guardrailId);
        Assert.False(guardrail.IsActive);
    }

    [Fact]
    public async Task CheckGuardrails_WithInactiveGuardrail_DoesNotTrigger()
    {
        await AuthorizeAsAdmin();
        var (experimentId, guardrailId) = await SetupRunningExperimentWithGuardrail(threshold: 0.5);

        using (var scope = m_Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var guardrail = await db.Guardrails.FirstAsync(g => g.Id == guardrailId);
            guardrail.IsActive = false;
            await db.SaveChangesAsync();
        }

        await InsertDecisionsAndEvents(experimentId, exposureCount: 10, errorCount: 8);

        await CreateMonitoringService().CheckGuardrails(CancellationToken.None);

        var experiment = await GetExperiment(experimentId);
        Assert.Equal(EExperimentStatus.Running, experiment.Status);
        Assert.Empty(await GetTriggerHistory(experimentId));
    }

    [Fact]
    public async Task CheckGuardrails_WithNoDecisionsInWindow_DoesNotTrigger()
    {
        await AuthorizeAsAdmin();
        var (experimentId, _) = await SetupRunningExperimentWithGuardrail(threshold: 0.5);

        await CreateMonitoringService().CheckGuardrails(CancellationToken.None);

        var experiment = await GetExperiment(experimentId);
        Assert.Equal(EExperimentStatus.Running, experiment.Status);
        Assert.Empty(await GetTriggerHistory(experimentId));
    }

    [Fact]
    public async Task CheckGuardrails_WhenRollbackTriggered_SetsCompletedAtAndOutcomeComment()
    {
        await AuthorizeAsAdmin();
        var (experimentId, _) = await SetupRunningExperimentWithGuardrail(
            threshold: 0.5, action: EGuardrailAction.RollbackToControl);
        await InsertDecisionsAndEvents(experimentId, exposureCount: 10, errorCount: 8);

        await CreateMonitoringService().CheckGuardrails(CancellationToken.None);

        var experiment = await GetExperiment(experimentId);
        Assert.NotNull(experiment.CompletedAt);
        Assert.NotNull(experiment.OutcomeComment);
        Assert.Contains("guardrail", experiment.OutcomeComment, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckGuardrails_WithNonRunningExperiment_IsSkipped()
    {
        await AuthorizeAsAdmin();
        var (experimentId, _) = await SetupRunningExperimentWithGuardrail(threshold: 0.5);
        await InsertDecisionsAndEvents(experimentId, exposureCount: 10, errorCount: 8);

        using (var scope = m_Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var exp = await db.Experiments.FirstAsync(e => e.Id == experimentId);
            exp.Status = EExperimentStatus.Paused;
            await db.SaveChangesAsync();
        }

        await CreateMonitoringService().CheckGuardrails(CancellationToken.None);

        Assert.Empty(await GetTriggerHistory(experimentId));
    }
}