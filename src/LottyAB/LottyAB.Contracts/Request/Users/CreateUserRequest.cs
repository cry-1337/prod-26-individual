using LottyAB.Domain.Enums;

namespace LottyAB.Contracts.Request.Users;

public record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    EUserRole Role = EUserRole.Viewer);