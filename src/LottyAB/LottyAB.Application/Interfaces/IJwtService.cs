using LottyAB.Domain.Entities;

namespace LottyAB.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(UserEntity user);
}