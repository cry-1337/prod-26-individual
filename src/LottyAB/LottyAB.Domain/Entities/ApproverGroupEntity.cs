namespace LottyAB.Domain.Entities;

public class ApproverGroupEntity : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int ApproversToStart { get; set; }

    public ICollection<UserEntity> Approvers { get; set; } = new List<UserEntity>();
}