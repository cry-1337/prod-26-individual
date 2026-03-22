using LottyAB.Domain.Enums;

namespace LottyAB.Contracts.Request.Users;

public record UpdateUserRequest(
    string? Name = null,
    string? Email = null,
    string? Password = null,
    EUserRole? Role = null,
    bool? IsActive = null);