namespace AuthSystem.Application.Interfaces;

public interface IRefreshTokenHasher
{
    string Hash(string rawToken);
}
