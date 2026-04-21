using AuthSystem.Domain.Entities;

namespace AuthSystem.Application.Interfaces;

public interface IJwtTokenService
{
    (string Token, int ExpiresInSeconds) GenerateAccessToken(User user);
    (string RawToken, DateTime ExpiresAt) GenerateRefreshToken();
}
