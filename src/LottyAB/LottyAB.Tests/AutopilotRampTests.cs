using LottyAB.Application.Interfaces;
using LottyAB.Contracts.Request.Autopilot;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using LottyAB.Infrastructure.Services;
using LottyAB.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LottyAB.Tests;

public class AutopilotRampTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    private readonly BaseTestFactory m_Factory = factory;

    private AutopilotRampService CreateAutopilotService() =>
        new(m_Factory.Services,
            m_Factory.Services.GetRequiredService<ILogger<AutopilotRampService>>());

    private async Task<Guid> CreateSmallAudienceRunningExperiment()
    {
        var (_, experimentId) = await CreateRunningExperimentWithFlag(
            audienceFraction: 0.05,
            controlWeight: 0.025,
            treatmentWeight: 0.025);
        return experimentId;
    }

    private async Task CreateAutopilotPlan(
        Guid experimentId,
        double[]? steps = null,
        int minImpressions = 2,
        int minMinutes = 0,
        ERampSafetyAction safetyAction = ERampSafetyAction.Pause)
    {
        steps ??= [0.25, 0.50, 1.0];
        var request = new CreateRampPlanRequest(steps, minImpressions, minMinutes, safetyAction);
        var response = await Client.PostAsJsonAsync($"/api/experiments/{experimentId}/autopilot", request);
        response.EnsureSuccessStatusCode();
    }

    private async Task InsertDecisions(Guid experimentId, int count)
    {
        using var scope = m_Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var experiment = await db.Experiments.FirstAsync(e => e.Id == experimentId);
        var decisions = Enumerable.Range(0, count).Select(_ => new DecisionEntity
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
    }

    private async Task SetStepEnteredAt(Guid experimentId, DateTime stepEnteredAt)
    {
        using var scope = m_Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var plan = await db.RampPlans.FirstAsync(rp => rp.ExperimentId == experimentId);
        plan.StepEnteredAt = stepEnteredAt;
        await db.SaveChangesAsync();
    }

    private async Task<ExperimentEntity> GetExperiment(Guid id)
    {
        using var scope = m_Factory.Services.CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<IApplicationDbContext>()
            .Experiments.FirstAsync(e => e.Id == id);
    }

    private async Task<RampPlanEntity> GetRampPlan(Guid experimentId)
    {
        using var scope = m_Factory.Services.CreateScope();
        return await scope.ServiceProvider
            .GetRequiredService<IApplicationDbContext>()
            .RampPlans
            .Include(rp => rp.History)
            .FirstAsync(rp => rp.ExperimentId == experimentId);
    }

    private async Task InsertGuardrailTrigger(Guid experimentId, DateTime triggeredAt)
    {
        using var scope = m_Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var guardrail = new GuardrailEntity
        {
            ExperimentId = experimentId,
            MetricKey = "error_rate",
            Threshold = 0.5,
            ObservationWindowMinutes = 60,
            Action = EGuardrailAction.Pause,
            IsActive = true
        };
        db.Guardrails.Add(guardrail);
        await db.SaveChangesAsync();

        db.GuardrailTriggerHistory.Add(new GuardrailTriggerHistoryEntity
        {
            GuardrailId = guardrail.Id,
            ExperimentId = experimentId,
            MetricKey = "error_rate",
            Threshold = 0.5,
            ActualValue = 0.8,
            ActionTaken = EGuardrailAction.Pause,
            TriggeredAt = triggeredAt,
            ObservationWindowMinutes = 60
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Autopilot_AdvancesStep_WhenAllGatesPass()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateSmallAudienceRunningExperiment();
        await CreateAutopilotPlan(experimentId, minImpressions: 2, minMinutes: 0);
        await SetStepEnteredAt(experimentId, DateTime.UtcNow.AddHours(-1));
        await InsertDecisions(experimentId, 3);

        await CreateAutopilotService().CheckRampPlans(CancellationToken.None);

        var experiment = await GetExperiment(experimentId);
        var plan = await GetRampPlan(experimentId);
        Assert.Equal(0.25, experiment.AudienceFraction, 5);
        Assert.Equal(1, plan.CurrentStepIndex);
    }

    [Fact]
    public async Task Autopilot_DoesNotAdvance_WhenInsufficientImpressions()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateSmallAudienceRunningExperiment();
        await CreateAutopilotPlan(experimentId, minImpressions: 10, minMinutes: 0);
        await SetStepEnteredAt(experimentId, DateTime.UtcNow.AddHours(-1));

        await CreateAutopilotService().CheckRampPlans(CancellationToken.None);

        var experiment = await GetExperiment(experimentId);
        var plan = await GetRampPlan(experimentId);
        Assert.Equal(0.05, experiment.AudienceFraction, 5);
        Assert.Equal(0, plan.CurrentStepIndex);
    }

    [Fact]
    public async Task Autopilot_DoesNotAdvance_WhenTimeTooShort()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateSmallAudienceRunningExperiment();
        await CreateAutopilotPlan(experimentId, minImpressions: 0, minMinutes: 60);

        await CreateAutopilotService().CheckRampPlans(CancellationToken.None);

        var experiment = await GetExperiment(experimentId);
        var plan = await GetRampPlan(experimentId);
        Assert.Equal(0.05, experiment.AudienceFraction, 5);
        Assert.Equal(0, plan.CurrentStepIndex);
    }

    [Fact]
    public async Task Autopilot_PausesExperiment_WhenGuardrailTriggered_SafetyActionPause()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateSmallAudienceRunningExperiment();
        await CreateAutopilotPlan(experimentId, safetyAction: ERampSafetyAction.Pause);
        var plan = await GetRampPlan(experimentId);
        await InsertGuardrailTrigger(experimentId, plan.StepEnteredAt.AddMinutes(1));

        await CreateAutopilotService().CheckRampPlans(CancellationToken.None);

        var experiment = await GetExperiment(experimentId);
        Assert.Equal(EExperimentStatus.Paused, experiment.Status);
    }

    [Fact]
    public async Task Autopilot_StepsBack_WhenGuardrailTriggered_SafetyActionStepBack()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateSmallAudienceRunningExperiment();
        await CreateAutopilotPlan(experimentId, steps: [0.25, 0.50, 1.0], safetyAction: ERampSafetyAction.StepBack);

        DateTime stepEnteredAt;
        using (var scope = m_Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var rampPlan = await db.RampPlans.FirstAsync(rp => rp.ExperimentId == experimentId);
            var experiment = await db.Experiments.Include(e => e.Variants).FirstAsync(e => e.Id == experimentId);
            stepEnteredAt = DateTime.UtcNow.AddHours(-2);
            rampPlan.CurrentStepIndex = 1;
            rampPlan.StepEnteredAt = stepEnteredAt;
            experiment.AudienceFraction = 0.25;
            foreach (var v in experiment.Variants)
                v.Weight = v.Weight / 0.05 * 0.25;
            await db.SaveChangesAsync();
        }

        await InsertGuardrailTrigger(experimentId, stepEnteredAt.AddMinutes(1));

        await CreateAutopilotService().CheckRampPlans(CancellationToken.None);

        var planAfter = await GetRampPlan(experimentId);
        var experimentAfter = await GetExperiment(experimentId);
        Assert.Equal(0, planAfter.CurrentStepIndex);
        Assert.True(experimentAfter.AudienceFraction < 0.25);
    }

    [Fact]
    public async Task Autopilot_RecordsHistory_WhenAdvancing()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateSmallAudienceRunningExperiment();
        await CreateAutopilotPlan(experimentId, minImpressions: 2, minMinutes: 0);
        await SetStepEnteredAt(experimentId, DateTime.UtcNow.AddHours(-1));
        await InsertDecisions(experimentId, 3);

        await CreateAutopilotService().CheckRampPlans(CancellationToken.None);

        var plan = await GetRampPlan(experimentId);
        Assert.Single(plan.History);
        Assert.Equal(ERampPlanAction.Advanced, plan.History.First().Action);
    }

    [Fact]
    public async Task Autopilot_IsSkipped_WhenDisabled()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateSmallAudienceRunningExperiment();
        await CreateAutopilotPlan(experimentId, minImpressions: 2, minMinutes: 0);
        await SetStepEnteredAt(experimentId, DateTime.UtcNow.AddHours(-1));
        await InsertDecisions(experimentId, 3);

        var disableResp = await Client.PostAsync($"/api/experiments/{experimentId}/autopilot/disable", null);
        disableResp.EnsureSuccessStatusCode();

        await CreateAutopilotService().CheckRampPlans(CancellationToken.None);

        var experiment = await GetExperiment(experimentId);
        var plan = await GetRampPlan(experimentId);
        Assert.Equal(0.05, experiment.AudienceFraction, 5);
        Assert.Empty(plan.History);
    }

    [Fact]
    public async Task Autopilot_MarksCompleted_WhenAtFinalStep()
    {
        await AuthorizeAsAdmin();
        var experimentId = await CreateSmallAudienceRunningExperiment();
        await CreateAutopilotPlan(experimentId, steps: [0.25, 0.50, 1.0]);

        using (var scope = m_Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            var rampPlan = await db.RampPlans.FirstAsync(rp => rp.ExperimentId == experimentId);
            rampPlan.CurrentStepIndex = 3;
            await db.SaveChangesAsync();
        }

        await CreateAutopilotService().CheckRampPlans(CancellationToken.None);

        var plan = await GetRampPlan(experimentId);
        Assert.True(plan.IsCompleted);
        Assert.Single(plan.History);
        Assert.Equal(ERampPlanAction.Completed, plan.History.First().Action);
    }
}