using AuthSystem.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace AuthSystem.UnitTests.Infrastructure.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ReturnsBCryptString()
    {
        var hash = _hasher.Hash("Passw0rd");
        hash.Should().StartWith("$2");
        hash.Length.Should().BeGreaterOrEqualTo(59);
    }

    [Fact]
    public void Hash_ProducesDifferentOutputForSameInput()
    {
        var hash1 = _hasher.Hash("Passw0rd");
        var hash2 = _hasher.Hash("Passw0rd");
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Passw0rd");
        _hasher.Verify("Passw0rd", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Passw0rd");
        _hasher.Verify("WrongPass", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithMalformedHash_ReturnsFalse()
    {
        _hasher.Verify("Passw0rd", "not-a-bcrypt-hash").Should().BeFalse();
    }

    [Fact]
    public void Verify_WithEmptyHash_ReturnsFalse()
    {
        _hasher.Verify("Passw0rd", "").Should().BeFalse();
    }
}
