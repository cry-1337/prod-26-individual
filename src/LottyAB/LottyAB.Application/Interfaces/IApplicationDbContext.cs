using LottyAB.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<UserEntity> Users { get; set; }
    DbSet<ExperimentEntity> Experiments { get; set; }
    DbSet<ExperimentVersionEntity> ExperimentVersions { get; set; }
    DbSet<FeatureFlagEntity> FeatureFlags { get; set; }
    DbSet<VariantEntity> Variants { get; set; }
    DbSet<ExperimentReviewEntity> ExperimentReviews { get; set; }
    DbSet<DecisionEntity> Decisions { get; set; }
    DbSet<EventTypeEntity> EventTypes { get; set; }
    DbSet<EventEntity> Events { get; set; }
    DbSet<MetricDefinitionEntity> MetricDefinitions { get; set; }
    DbSet<GuardrailEntity> Guardrails { get; set; }
    DbSet<GuardrailTriggerHistoryEntity> GuardrailTriggerHistory { get; set; }
    DbSet<SubjectParticipationEntity> SubjectParticipation { get; set; }
    DbSet<ApproverGroupEntity> ApproverGroups { get; set; }
    DbSet<RampPlanEntity> RampPlans { get; set; }
    DbSet<RampPlanHistoryEntity> RampPlanHistory { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}