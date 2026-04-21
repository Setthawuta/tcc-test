using AuthSystem.Application.Interfaces;

namespace AuthSystem.Infrastructure.Security;

public class Sha256RefreshTokenHasher : IRefreshTokenHasher
{
    public string Hash(string rawToken) => RefreshTokenHasher.Hash(rawToken);
}
