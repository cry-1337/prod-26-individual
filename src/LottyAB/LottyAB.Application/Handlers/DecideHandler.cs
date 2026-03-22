using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using LottyAB.Application.Commands;
using LottyAB.Application.Exceptions;
using LottyAB.Application.Interfaces;
using LottyAB.Contracts.Responses;
using LottyAB.Domain.Entities;
using LottyAB.Domain.Enums;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace LottyAB.Application.Handlers;

public class DecideHandler(
    IApplicationDbContext dbContext,
    IHashVariantSelector hashVariantSelector,
    ITargetingEvaluator targetingEvaluator,
    IValueTypeConverter valueTypeConverter,
    IDistributedCache cache) : IRequestHandler<DecideCommand, DecisionResponse>
{
    private const int MaxConcurrentExperiments = 3;
    private static readonly DistributedCacheEntryOptions m_SCacheEntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
    };

    public async Task<DecisionResponse> Handle(DecideCommand request, CancellationToken cancellationToken)
    {
        var cacheKey = $"flag:{request.FeatureFlagKey}";
        FeatureFlagEntity? featureFlag = null;

        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            var entry = JsonSerializer.Deserialize<FeatureFlagCacheEntry>(cached);
            if (entry is not null)
                featureFlag = entry.Adapt<FeatureFlagEntity>();
        }

        if (featureFlag is null)
        {
            featureFlag = await dbContext.FeatureFlags
                .AsNoTracking()
                .Include(x => x.Experiments)
                .ThenInclude(x => x.Variants)
                .FirstOrDefaultAsync(x => x.Key == request.FeatureFlagKey, cancellationToken);

            if (featureFlag is not null)
            {
                var serialized = JsonSerializer.Serialize(featureFlag.Adapt<FeatureFlagCacheEntry>());
                await cache.SetStringAsync(cacheKey, serialized, m_SCacheEntryOptions, cancellationToken);
            }
        }

        if (featureFlag is null)
            throw new NotFoundException("FeatureFlag", request.FeatureFlagKey);

        var experiment = featureFlag.Experiments
            .FirstOrDefault(e => e.FeatureFlagId == featureFlag.Id && e.Status == EExperimentStatus.Running);

        var defaultValue = await CreateDefaultDecision(
            request.SubjectId, featureFlag, request.SubjectAttributes, cancellationToken);

        if (experiment == null || !targetingEvaluator.EvaluateRule(experiment.TargetingRule, request.SubjectAttributes))
            return defaultValue;

        if (experiment.ConflictPolicy == EConflictPolicy.MutualExclusion && !string.IsNullOrEmpty(experiment.ConflictDomains))
        {
            if (!await IsConflictDomainWinner(experiment, request.SubjectId, cancellationToken))
                return defaultValue;
        }

        var variant = hashVariantSelector.SelectVariant(request.SubjectId, experiment);

        if (variant == null)
            return defaultValue;

        var recentParticipations = await dbContext.SubjectParticipation
            .Where(p => p.SubjectId == request.SubjectId && p.ParticipatedAt > DateTime.UtcNow.AddDays(-30))
            .ToListAsync(cancellationToken);

        if (recentParticipations.Count >= MaxConcurrentExperiments)
            return defaultValue;

        if (recentParticipations.Any(p => p.ExperimentId == experiment.Id))
            return await CreateVariantDecision(
                request.SubjectId, featureFlag, experiment, variant, request.SubjectAttributes, cancellationToken);
        await dbContext.SubjectParticipation.AddAsync(new SubjectParticipationEntity
        {
            SubjectId = request.SubjectId,
            ExperimentId = experiment.Id,
            ParticipatedAt = DateTime.UtcNow
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateVariantDecision(
                request.SubjectId, featureFlag, experiment, variant, request.SubjectAttributes, cancellationToken);
    }

    private async Task<bool> IsConflictDomainWinner(ExperimentEntity experiment, string subjectId, CancellationToken ct)
    {
        var currentDomains = experiment.ConflictDomains!
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allRunning = await dbContext.Experiments
            .AsNoTracking()
            .Where(e => e.Status == EExperimentStatus.Running &&
                        e.ConflictPolicy == EConflictPolicy.MutualExclusion &&
                        e.Id != experiment.Id &&
                        e.ConflictDomains != null)
            .ToListAsync(ct);

        var competitors = allRunning
            .Where(e => e.ConflictDomains!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(d => currentDomains.Contains(d)))
            .ToList();

        if (competitors.Count == 0) return true;

        var maxCompetitorPriority = competitors.Max(e => e.Priority);

        if (maxCompetitorPriority > experiment.Priority) return false;
        if (experiment.Priority > maxCompetitorPriority) return true;

        var tied = competitors
            .Where(e => e.Priority == experiment.Priority)
            .Append(experiment)
            .OrderBy(e => e.Id)
            .ToList();

        var winner = tied
            .Select(e =>
            {
                var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{subjectId}:{e.Id:N}"));
                return (experiment: e, score: BitConverter.ToUInt64(bytes, 0));
            })
            .MaxBy(x => x.score);

        return winner.experiment.Id == experiment.Id;
    }

    private async Task<DecisionResponse> CreateDefaultDecision(string subjectId, FeatureFlagEntity featureFlag,
        Dictionary<string, object>? subjectAttributes, CancellationToken cancellationToken)
    {
        var decision = new DecisionEntity
        {
            SubjectId = subjectId,
            FeatureFlagId = featureFlag.Id,
            IsDefault = true,
            VariantValue = featureFlag.DefaultValue,
            Timestamp = DateTime.UtcNow
        };

        decision.SetSubjectAttributes(subjectAttributes);

        dbContext.Decisions.Add(decision);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DecisionResponse
        {
            DecisionId = decision.Id,
            VariantKey = featureFlag.Key,
            VariantValue = featureFlag.DefaultValue,
            TypedValue = valueTypeConverter.ConvertValue(featureFlag.DefaultValue, featureFlag.ValueType),
            ValueType = featureFlag.ValueType,
            IsDefault = true,
            FeatureFlagKey = featureFlag.Key
        };
    }

    private async Task<DecisionResponse> CreateVariantDecision(string subjectId, FeatureFlagEntity featureFlag, ExperimentEntity experiment,
        VariantEntity variant, Dictionary<string, object>? subjectAttributes, CancellationToken cancellationToken)
    {
        var decision = new DecisionEntity
        {
            SubjectId = subjectId,
            FeatureFlagId = featureFlag.Id,
            ExperimentId = experiment.Id,
            VariantId = variant.Id,
            IsDefault = false,
            VariantValue = variant.Value,
            Timestamp = DateTime.UtcNow
        };

        decision.SetSubjectAttributes(subjectAttributes);

        dbContext.Decisions.Add(decision);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DecisionResponse
        {
            DecisionId = decision.Id,
            VariantKey = variant.Name,
            VariantValue = variant.Value,
            TypedValue = valueTypeConverter.ConvertValue(variant.Value, featureFlag.ValueType),
            ValueType = featureFlag.ValueType,
            IsDefault = false,
            FeatureFlagKey = featureFlag.Key,
            ExperimentId = experiment.Id,
            VariantId = variant.Id
        };
    }

    private sealed class FeatureFlagCacheEntry
    {
        public Guid Id { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public EFeatureFlagType ValueType { get; init; }
        public string DefaultValue { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public List<ExperimentCacheEntry> Experiments { get; init; } = [];
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private sealed class ExperimentCacheEntry
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid FeatureFlagId { get; set; }
        public EExperimentStatus Status { get; set; }
        public int Version { get; set; }
        public double AudienceFraction { get; set; }
        public string? TargetingRule { get; set; }
        public Guid OwnerId { get; set; }
        public Guid? ApproverGroupId { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public ECompletionOutcome? Outcome { get; set; }
        public string? OutcomeComment { get; set; }
        public string? PrimaryMetricKey { get; set; }
        public string? GuardrailMetricKeys { get; set; }
        public string? ConflictDomains { get; set; }
        public EConflictPolicy? ConflictPolicy { get; set; }
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<VariantCacheEntry> Variants { get; set; } = [];
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private sealed class VariantCacheEntry
    {
        public Guid Id { get; set; }
        public Guid ExperimentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public double Weight { get; set; }
        public bool IsControl { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}