using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using LottyAB.Infrastructure.Services;
using LottyAB.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LottyAB.Tests;

public class EventAttributionTests(BaseTestFactory factory) : BaseIntegrationTest(factory)
{
    private readonly BaseTestFactory m_Factory = factory;

    private EventAttributionService CreateAttributionService() =>
        new(m_Factory.Services,
            m_Factory.Services.GetRequiredService<ILogger<EventAttributionService>>());

    private async Task<(Guid ExperimentId, Guid FeatureFlagId)> GetExperimentDetails()
    {
        var experimentId = await CreateExperiment();
        using var scope = m_Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var exp = await db.Experiments.FirstAsync(e => e.Id == experimentId);
        return (experimentId, exp.FeatureFlagId);
    }

    private async Task<Guid> InsertDecision(Guid experimentId, Guid featureFlagId)
    {
        using var scope = m_Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var decision = new DecisionEntity
        {
            ExperimentId = experimentId,
            FeatureFlagId = featureFlagId,
            SubjectId = Guid.NewGuid().ToString(),
            VariantValue = "treatment",
            IsDefault = false,
            Timestamp = DateTime.UtcNow
        };
        db.Decisions.Add(decision);
        await db.SaveChangesAsync();
        return decision.Id;
    }

    private async Task InsertEvent(
        Guid decisionId,
        string eventKey,
        bool isAttributed = false,
        DateTime? timestamp = null)
    {
        using var scope = m_Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var eventType = await db.EventTypes.FirstAsync(et => et.EventKey == eventKey);
        db.Events.Add(new EventEntity
        {
            EventId = Guid.NewGuid().ToString(),
            DecisionId = decisionId,
            EventTypeId = eventType.Id,
            SubjectId = Guid.NewGuid().ToString(),
            IsAttributed = isAttributed,
            EventTimestamp = timestamp ?? DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private async Task<EventEntity> GetEvent(Guid decisionId, string eventKey)
    {
        using var scope = m_Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        return await db.Events
            .Include(e => e.EventType)
            .FirstAsync(e => e.DecisionId == decisionId && e.EventType.EventKey == eventKey);
    }

    [Fact]
    public async Task ProcessUnattributedEvents_WhenExposureExists_AttributesConversionEvent()
    {
        await AuthorizeAsAdmin();
        var (experimentId, featureFlagId) = await GetExperimentDetails();
        var decisionId = await InsertDecision(experimentId, featureFlagId);

        await InsertEvent(decisionId, "exposure");
        await InsertEvent(decisionId, "conversion");

        await CreateAttributionService().ProcessUnattributedEvents(CancellationToken.None);

        var conversion = await GetEvent(decisionId, "conversion");
        Assert.True(conversion.IsAttributed);
    }

    [Fact]
    public async Task ProcessUnattributedEvents_WhenNoExposure_EventRemainsUnattributed()
    {
        await AuthorizeAsAdmin();
        var (experimentId, featureFlagId) = await GetExperimentDetails();
        var decisionId = await InsertDecision(experimentId, featureFlagId);

        await InsertEvent(decisionId, "conversion");

        await CreateAttributionService().ProcessUnattributedEvents(CancellationToken.None);

        var conversion = await GetEvent(decisionId, "conversion");
        Assert.False(conversion.IsAttributed);
    }

    [Fact]
    public async Task ProcessUnattributedEvents_WhenEventOlderThanSevenDays_IsSkipped()
    {
        await AuthorizeAsAdmin();
        var (experimentId, featureFlagId) = await GetExperimentDetails();
        var decisionId = await InsertDecision(experimentId, featureFlagId);

        await InsertEvent(decisionId, "exposure");
        await InsertEvent(decisionId, "conversion", timestamp: DateTime.UtcNow.AddDays(-8));

        await CreateAttributionService().ProcessUnattributedEvents(CancellationToken.None);

        var conversion = await GetEvent(decisionId, "conversion");
        Assert.False(conversion.IsAttributed);
    }

    [Fact]
    public async Task ProcessUnattributedEvents_WhenEventAlreadyAttributed_IsNotTouched()
    {
        await AuthorizeAsAdmin();
        var (experimentId, featureFlagId) = await GetExperimentDetails();
        var decisionId = await InsertDecision(experimentId, featureFlagId);

        await InsertEvent(decisionId, "conversion", isAttributed: true);

        var ex = await Record.ExceptionAsync(
            () => CreateAttributionService().ProcessUnattributedEvents(CancellationToken.None));

        Assert.Null(ex);
        var conversion = await GetEvent(decisionId, "conversion");
        Assert.True(conversion.IsAttributed);
    }

    [Fact]
    public async Task ProcessUnattributedEvents_WithMixedDecisions_AttributesOnlyExposed()
    {
        await AuthorizeAsAdmin();
        var (experimentId, featureFlagId) = await GetExperimentDetails();

        var decisionA = await InsertDecision(experimentId, featureFlagId);
        await InsertEvent(decisionA, "exposure");
        await InsertEvent(decisionA, "conversion");

        var decisionB = await InsertDecision(experimentId, featureFlagId);
        await InsertEvent(decisionB, "conversion");

        await CreateAttributionService().ProcessUnattributedEvents(CancellationToken.None);

        var convA = await GetEvent(decisionA, "conversion");
        var convB = await GetEvent(decisionB, "conversion");
        Assert.True(convA.IsAttributed);
        Assert.False(convB.IsAttributed);
    }

    [Fact]
    public async Task ProcessUnattributedEvents_ExposureEvents_AreNeverAttributed()
    {
        await AuthorizeAsAdmin();
        var (experimentId, featureFlagId) = await GetExperimentDetails();
        var decisionId = await InsertDecision(experimentId, featureFlagId);

        await InsertEvent(decisionId, "exposure");

        await CreateAttributionService().ProcessUnattributedEvents(CancellationToken.None);

        var exposure = await GetEvent(decisionId, "exposure");
        Assert.False(exposure.IsAttributed);
    }

    [Fact]
    public async Task ProcessUnattributedEvents_WhenErrorEventHasExposure_IsAttributed()
    {
        await AuthorizeAsAdmin();
        var (experimentId, featureFlagId) = await GetExperimentDetails();
        var decisionId = await InsertDecision(experimentId, featureFlagId);

        await InsertEvent(decisionId, "exposure");
        await InsertEvent(decisionId, "error");

        await CreateAttributionService().ProcessUnattributedEvents(CancellationToken.None);

        var error = await GetEvent(decisionId, "error");
        Assert.True(error.IsAttributed);
    }
}