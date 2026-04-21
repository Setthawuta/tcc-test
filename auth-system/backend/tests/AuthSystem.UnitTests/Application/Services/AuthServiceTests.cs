using AuthSystem.Application.Common;
using AuthSystem.Application.DTOs;
using AuthSystem.Application.Interfaces;
using AuthSystem.Application.Services;
using AuthSystem.Application.Validators;
using AuthSystem.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AuthSystem.UnitTests.Application.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<IRefreshTokenHasher> _refreshHasher = new();

    private AuthService CreateService()
    {
        _refreshHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns<string>(s => $"hash:{s}");
        return new AuthService(
            _userRepo.Object,
            _refreshRepo.Object,
            _hasher.Object,
            _jwt.Object,
            _refreshHasher.Object,
            new RegisterRequestValidator(),
            new LoginRequestValidator(),
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task Register_WithValidInput_PersistsUser()
    {
        _userRepo.Setup(r => r.UsernameExistsAsync("alice01", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _hasher.Setup(h => h.Hash("Passw0rd")).Returns("hashed");

        var result = await CreateService().RegisterAsync(new RegisterRequest
        {
            Username = "alice01",
            Password = "Passw0rd",
            ConfirmPassword = "Passw0rd"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Username.Should().Be("alice01");
        _userRepo.Verify(r => r.AddAsync(It.Is<User>(u => u.PasswordHash == "hashed"), It.IsAny<CancellationToken>()), Times.Once);
        _userRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_WithInvalidInput_ReturnsValidationError()
    {
        var result = await CreateService().RegisterAsync(new RegisterRequest
        {
            Username = "ab",
            Password = "x",
            ConfirmPassword = "y",
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultError.Validation);
        _userRepo.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_WhenUsernameExists_ReturnsConflict()
    {
        _userRepo.Setup(r => r.UsernameExistsAsync("alice01", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await CreateService().RegisterAsync(new RegisterRequest
        {
            Username = "alice01",
            Password = "Passw0rd",
            ConfirmPassword = "Passw0rd",
        });

        result.ErrorType.Should().Be(ResultError.Conflict);
    }

    [Fact]
    public async Task Login_WithValidCredentials_IssuesTokenPair()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "alice01", PasswordHash = "hash" };
        _userRepo.Setup(r => r.GetByUsernameAsync("alice01", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Passw0rd", "hash")).Returns(true);
        _jwt.Setup(j => j.GenerateAccessToken(user)).Returns(("access", 3600));
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns(("raw", DateTime.UtcNow.AddDays(7)));

        var result = await CreateService().LoginAsync(new LoginRequest { Username = "alice01", Password = "Passw0rd" });

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access");
        result.Value.RefreshToken.Should().Be("raw");
        result.Value.ExpiresIn.Should().Be(3600);
        _refreshRepo.Verify(r => r.AddAsync(It.Is<RefreshToken>(t => t.TokenHash == "hash:raw"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_ReturnsUnauthorized()
    {
        var result = await CreateService().LoginAsync(new LoginRequest());

        result.ErrorType.Should().Be(ResultError.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownUsername_ReturnsUnauthorized()
    {
        _userRepo.Setup(r => r.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateService().LoginAsync(new LoginRequest { Username = "ghost", Password = "x" });

        result.ErrorType.Should().Be(ResultError.Unauthorized);
    }

    [Fact]
    public async Task Login_WithLockedUser_ReturnsUnauthorized()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "alice01", LockedUntil = DateTime.UtcNow.AddMinutes(5) };
        _userRepo.Setup(r => r.GetByUsernameAsync("alice01", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateService().LoginAsync(new LoginRequest { Username = "alice01", Password = "x" });

        result.ErrorType.Should().Be(ResultError.Unauthorized);
        _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithWrongPassword_IncrementsFailedCountAndReturnsUnauthorized()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "alice01", PasswordHash = "hash", FailedLoginCount = 1 };
        _userRepo.Setup(r => r.GetByUsernameAsync("alice01", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), "hash")).Returns(false);

        var result = await CreateService().LoginAsync(new LoginRequest { Username = "alice01", Password = "wrong" });

        result.ErrorType.Should().Be(ResultError.Unauthorized);
        user.FailedLoginCount.Should().Be(2);
        _userRepo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_ResetsFailedCountOnSuccess()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "alice01", PasswordHash = "hash", FailedLoginCount = 3 };
        _userRepo.Setup(r => r.GetByUsernameAsync("alice01", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(h => h.Verify("Passw0rd", "hash")).Returns(true);
        _jwt.Setup(j => j.GenerateAccessToken(user)).Returns(("a", 60));
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns(("r", DateTime.UtcNow.AddDays(7)));

        await CreateService().LoginAsync(new LoginRequest { Username = "alice01", Password = "Passw0rd" });

        user.FailedLoginCount.Should().Be(0);
        user.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Refresh_WithEmptyToken_ReturnsUnauthorized()
    {
        var result = await CreateService().RefreshAsync("");
        result.ErrorType.Should().Be(ResultError.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ReturnsUnauthorized()
    {
        _refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((RefreshToken?)null);

        var result = await CreateService().RefreshAsync("raw");

        result.ErrorType.Should().Be(ResultError.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithExpiredToken_ReturnsUnauthorized()
    {
        var stored = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "hash:raw",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
        };
        _refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var result = await CreateService().RefreshAsync("raw");

        result.ErrorType.Should().Be(ResultError.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_RevokesAllForUser()
    {
        var userId = Guid.NewGuid();
        var stored = new RefreshToken
        {
            UserId = userId,
            TokenHash = "hash:raw",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow.AddMinutes(-1),
        };
        _refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var result = await CreateService().RefreshAsync("raw");

        result.ErrorType.Should().Be(ResultError.Unauthorized);
        _refreshRepo.Verify(r => r.RevokeAllForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_WithLockedUser_ReturnsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var stored = new RefreshToken
        {
            UserId = userId,
            TokenHash = "hash:raw",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        var user = new User { Id = userId, LockedUntil = DateTime.UtcNow.AddMinutes(5) };
        _refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateService().RefreshAsync("raw");

        result.ErrorType.Should().Be(ResultError.Unauthorized);
    }

    [Fact]
    public async Task Refresh_HappyPath_RotatesToken()
    {
        var userId = Guid.NewGuid();
        var stored = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash:raw",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        var user = new User { Id = userId, Username = "alice01" };

        _refreshRepo.SetupSequence(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored)
            .ReturnsAsync((RefreshToken?)null);

        _userRepo.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _jwt.Setup(j => j.GenerateAccessToken(user)).Returns(("newAccess", 3600));
        _jwt.Setup(j => j.GenerateRefreshToken()).Returns(("newRaw", DateTime.UtcNow.AddDays(7)));

        var result = await CreateService().RefreshAsync("raw");

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("newAccess");
        result.Value.RefreshToken.Should().Be("newRaw");
        stored.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_WithEmptyToken_IsNoop()
    {
        var result = await CreateService().LogoutAsync("");
        result.IsSuccess.Should().BeTrue();
        _refreshRepo.Verify(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Logout_WithActiveToken_Revokes()
    {
        var stored = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "hash:raw",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };
        _refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var result = await CreateService().LogoutAsync("raw");

        result.IsSuccess.Should().BeTrue();
        stored.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_WithAlreadyRevokedToken_IsIdempotent()
    {
        var stored = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "hash:raw",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            RevokedAt = DateTime.UtcNow.AddMinutes(-1),
        };
        _refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var result = await CreateService().LogoutAsync("raw");

        result.IsSuccess.Should().BeTrue();
        _refreshRepo.Verify(r => r.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Logout_WithUnknownToken_IsSuccess()
    {
        _refreshRepo.Setup(r => r.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((RefreshToken?)null);

        var result = await CreateService().LogoutAsync("raw");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUser_Found_ReturnsUser()
    {
        var user = new User { Id = Guid.NewGuid(), Username = "alice01" };
        _userRepo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateService().GetCurrentUserAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Username.Should().Be("alice01");
    }

    [Fact]
    public async Task GetCurrentUser_NotFound_ReturnsNotFound()
    {
        _userRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateService().GetCurrentUserAsync(Guid.NewGuid());

        result.ErrorType.Should().Be(ResultError.NotFound);
    }
}
