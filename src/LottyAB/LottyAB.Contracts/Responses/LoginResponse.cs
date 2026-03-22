using LottyAB.Domain.Entities;

namespace LottyAB.Contracts.Responses;

public class LoginResponse
{
    public required string AccessToken { get; set; }
    public required UserEntity UserData { get; set; }
}