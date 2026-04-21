using AuthSystem.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace AuthSystem.UnitTests.Infrastructure.Security;

public class Sha256RefreshTokenHasherTests
{
    private readonly Sha256RefreshTokenHasher _hasher = new();

    [Fact]
    public void Hash_DelegatesToRefreshTokenHasher()
    {
        _hasher.Hash("abc").Should().Be(RefreshTokenHasher.Hash("abc"));
    }

    [Fact]
    public void Hash_IsDeterministic()
    {
        _hasher.Hash("abc").Should().Be(_hasher.Hash("abc"));
    }
}
