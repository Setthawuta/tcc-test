using AuthSystem.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace AuthSystem.UnitTests.Infrastructure.Security;

public class RefreshTokenHasherTests
{
    [Fact]
    public void Hash_IsDeterministic()
    {
        RefreshTokenHasher.Hash("token-abc").Should().Be(RefreshTokenHasher.Hash("token-abc"));
    }

    [Fact]
    public void Hash_DifferentInputsProduceDifferentHashes()
    {
        var h1 = RefreshTokenHasher.Hash("token-a");
        var h2 = RefreshTokenHasher.Hash("token-b");
        h1.Should().NotBe(h2);
    }

    [Fact]
    public void Hash_Sha256ProducesHex64Chars()
    {
        var hash = RefreshTokenHasher.Hash("anything");
        hash.Length.Should().Be(64);
        hash.Should().MatchRegex("^[0-9A-F]+$");
    }

    [Fact]
    public void Hash_EmptyStringStillHashes()
    {
        RefreshTokenHasher.Hash("").Length.Should().Be(64);
    }
}
