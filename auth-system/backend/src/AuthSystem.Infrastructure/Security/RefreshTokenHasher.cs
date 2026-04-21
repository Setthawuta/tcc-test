using System.Security.Cryptography;
using System.Text;

namespace AuthSystem.Infrastructure.Security;

public static class RefreshTokenHasher
{
    public static string Hash(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
