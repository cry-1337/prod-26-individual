namespace LottyAB.Domain.Entities;

public class SubjectParticipationEntity
{
    public string SubjectId { get; set; } = string.Empty;
    public Guid ExperimentId { get; set; }
    public DateTime ParticipatedAt { get; set; }
}