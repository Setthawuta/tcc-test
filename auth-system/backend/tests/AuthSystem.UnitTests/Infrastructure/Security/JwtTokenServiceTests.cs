using System.IdentityModel.Tokens.Jwt;
using AuthSystem.Domain.Entities;
using AuthSystem.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthSystem.UnitTests.Infrastructure.Security;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateService(string? secret = null, int expMinutes = 60, int refreshDays = 7)
    {
        var options = Options.Create(new JwtOptions
        {
            Secret = secret ?? new string('x', 32),
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationMinutes = expMinutes,
            RefreshExpirationDays = refreshDays,
        });
        return new JwtTokenService(options);
    }

    [Fact]
    public void GenerateAccessToken_ProducesValidJwtWithClaims()
    {
        var service = CreateService();
        var user = new User { Id = Guid.NewGuid(), Username = "alice01" };

        var (token, expiresIn) = service.GenerateAccessToken(user);

        token.Should().NotBeNullOrWhiteSpace();
        expiresIn.Should().Be(60 * 60);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.Should().Be("test-issuer");
        jwt.Audiences.Should().Contain("test-audience");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "nameid" || c.Type.EndsWith("nameidentifier", StringComparison.Ordinal));
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_ExpirationMatchesConfig()
    {
        var service = CreateService(expMinutes: 15);
        var user = new User { Id = Guid.NewGuid(), Username = "alice01" };

        var (token, expiresIn) = service.GenerateAccessToken(user);

        expiresIn.Should().Be(15 * 60);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_ThrowsIfSecretTooShort()
    {
        var service = CreateService(secret: "tooshort");
        var user = new User { Id = Guid.NewGuid(), Username = "alice01" };

        Action act = () => service.GenerateAccessToken(user);
        act.Should().Throw<InvalidOperationException>().WithMessage("*32*");
    }

    [Fact]
    public void GenerateAccessToken_ThrowsIfSecretEmpty()
    {
        var service = CreateService(secret: "");
        var user = new User { Id = Guid.NewGuid(), Username = "alice01" };

        Action act = () => service.GenerateAccessToken(user);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GenerateRefreshToken_ProducesNonEmptyToken()
    {
        var (raw, expiresAt) = CreateService().GenerateRefreshToken();

        raw.Should().NotBeNullOrWhiteSpace();
        raw.Length.Should().BeGreaterThan(40);
        expiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesDifferentTokensEachTime()
    {
        var service = CreateService();
        var (r1, _) = service.GenerateRefreshToken();
        var (r2, _) = service.GenerateRefreshToken();
        r1.Should().NotBe(r2);
    }

    [Fact]
    public void GenerateRefreshToken_UsesUrlSafeBase64()
    {
        var (raw, _) = CreateService().GenerateRefreshToken();
        raw.Should().NotContain("+");
        raw.Should().NotContain("/");
        raw.Should().NotContain("=");
    }

    [Fact]
    public void GenerateRefreshToken_ExpiresAtHonorsConfig()
    {
        var (_, expiresAt) = CreateService(refreshDays: 30).GenerateRefreshToken();
        expiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromSeconds(5));
    }
}
