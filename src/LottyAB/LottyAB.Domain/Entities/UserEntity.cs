using System.Text.Json.Serialization;
using LottyAB.Domain.Enums;

namespace LottyAB.Domain.Entities;

public class UserEntity : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    public EUserRole Role { get; set; }

    public bool IsActive { get; set; } = true;
}