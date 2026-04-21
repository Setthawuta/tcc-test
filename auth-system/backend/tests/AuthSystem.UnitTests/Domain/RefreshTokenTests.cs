using AuthSystem.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace AuthSystem.UnitTests.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void NewToken_HasDefaultValues()
    {
        var t = new RefreshToken();
        t.Id.Should().NotBe(Guid.Empty);
        t.TokenHash.Should().BeEmpty();
        t.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        t.RevokedAt.Should().BeNull();
        t.ReplacedByTokenId.Should().BeNull();
    }

    [Fact]
    public void IsActive_NotRevoked_NotExpired_ReturnsTrue()
    {
        var t = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = null,
        };
        t.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_Revoked_ReturnsFalse()
    {
        var t = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow.AddMinutes(-1),
        };
        t.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_Expired_ReturnsFalse()
    {
        var t = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
            RevokedAt = null,
        };
        t.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithoutReplacement_SetsRevokedAt()
    {
        var t = new RefreshToken { ExpiresAt = DateTime.UtcNow.AddDays(1) };

        t.Revoke();

        t.RevokedAt.Should().NotBeNull();
        t.RevokedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        t.ReplacedByTokenId.Should().BeNull();
    }

    [Fact]
    public void Revoke_WithReplacement_SetsBoth()
    {
        var t = new RefreshToken { ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var replacement = Guid.NewGuid();

        t.Revoke(replacement);

        t.RevokedAt.Should().NotBeNull();
        t.ReplacedByTokenId.Should().Be(replacement);
    }
}
