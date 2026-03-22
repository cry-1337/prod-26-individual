using LottyAB.Application.Interfaces;
using LottyAB.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LottyAB.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<ExperimentEntity> Experiments { get; set; }
    public DbSet<ExperimentVersionEntity> ExperimentVersions { get; set; }
    public DbSet<FeatureFlagEntity> FeatureFlags { get; set; }
    public DbSet<VariantEntity> Variants { get; set; }
    public DbSet<ExperimentReviewEntity> ExperimentReviews { get; set; }
    public DbSet<DecisionEntity> Decisions { get; set; }
    public DbSet<EventTypeEntity> EventTypes { get; set; }
    public DbSet<EventEntity> Events { get; set; }
    public DbSet<MetricDefinitionEntity> MetricDefinitions { get; set; }
    public DbSet<GuardrailEntity> Guardrails { get; set; }
    public DbSet<GuardrailTriggerHistoryEntity> GuardrailTriggerHistory { get; set; }
    public DbSet<SubjectParticipationEntity> SubjectParticipation { get; set; }
    public DbSet<ApproverGroupEntity> ApproverGroups { get; set; }
    public DbSet<RampPlanEntity> RampPlans { get; set; }
    public DbSet<RampPlanHistoryEntity> RampPlanHistory { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SubjectParticipationEntity>()
            .HasKey(sp => new { sp.SubjectId, sp.ExperimentId });

        modelBuilder.Entity<FeatureFlagEntity>()
            .HasIndex(f => f.Key)
            .IsUnique();

        modelBuilder.Entity<ExperimentEntity>()
            .HasIndex(e => new { e.FeatureFlagId, e.Status });

        modelBuilder.Entity<SubjectParticipationEntity>()
            .HasIndex(sp => new { sp.SubjectId, sp.ParticipatedAt });

        modelBuilder.Entity<DecisionEntity>()
            .HasIndex(d => new { d.ExperimentId, d.Timestamp });

        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => e.DecisionId);

        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => new { e.IsAttributed, e.EventTimestamp });

        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => e.EventId)
            .IsUnique();

        modelBuilder.Entity<EventTypeEntity>()
            .HasIndex(et => et.EventKey)
            .IsUnique();

        modelBuilder.Entity<GuardrailTriggerHistoryEntity>()
            .HasIndex(g => g.ExperimentId);

        modelBuilder.Entity<RampPlanEntity>()
            .HasIndex(rp => rp.ExperimentId)
            .IsUnique();
    }
}