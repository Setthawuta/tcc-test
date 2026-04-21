using System.Security.Claims;
using AuthSystem.Api.Controllers;
using AuthSystem.Application.Common;
using AuthSystem.Application.DTOs;
using AuthSystem.Application.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AuthSystem.UnitTests.Api.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();

    private AuthController CreateController(ClaimsPrincipal? user = null)
    {
        var controller = new AuthController(_authService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
            }
        };
        return controller;
    }

    private static UserDto SampleUser() => new() { Id = Guid.NewGuid(), Username = "alice01", CreatedAt = DateTime.UtcNow };

    [Fact]
    public async Task Register_Success_Returns201WithUser()
    {
        var dto = SampleUser();
        _authService.Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(dto));

        var result = await CreateController().Register(new RegisterRequest(), CancellationToken.None);

        var status = result as ObjectResult;
        status.Should().NotBeNull();
        status!.StatusCode.Should().Be(201);
        status.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Register_Validation_Returns400()
    {
        _authService.Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure(ResultError.Validation, "bad"));

        var result = await CreateController().Register(new RegisterRequest(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_Conflict_Returns409()
    {
        _authService.Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure(ResultError.Conflict, "taken"));

        var result = await CreateController().Register(new RegisterRequest(), CancellationToken.None);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Register_UnknownFailure_Returns500()
    {
        _authService.Setup(s => s.RegisterAsync(It.IsAny<RegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure(ResultError.Unknown, "boom"));

        var result = await CreateController().Register(new RegisterRequest(), CancellationToken.None);

        (result as ObjectResult)!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Login_Success_Returns200WithAuthResponse()
    {
        var resp = new AuthResponse { AccessToken = "a", RefreshToken = "r", ExpiresIn = 3600, User = SampleUser() };
        _authService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Success(resp));

        var result = await CreateController().Login(new LoginRequest(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().Be(resp);
    }

    [Fact]
    public async Task Login_Failure_Returns401()
    {
        _authService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Failure(ResultError.Unauthorized, "Invalid"));

        var result = await CreateController().Login(new LoginRequest(), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Refresh_Success_Returns200()
    {
        var resp = new AuthResponse { AccessToken = "a", RefreshToken = "r", User = SampleUser() };
        _authService.Setup(s => s.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Success(resp));

        var result = await CreateController().Refresh(new RefreshTokenRequest { RefreshToken = "x" }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Refresh_Failure_Returns401()
    {
        _authService.Setup(s => s.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<AuthResponse>.Failure(ResultError.Unauthorized, "Invalid"));

        var result = await CreateController().Refresh(new RefreshTokenRequest(), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Logout_Always_Returns204()
    {
        _authService.Setup(s => s.LogoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await CreateController().Logout(new RefreshTokenRequest { RefreshToken = "x" }, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Logout_WithEmptyToken_StillReturns204()
    {
        _authService.Setup(s => s.LogoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await CreateController().Logout(new RefreshTokenRequest { RefreshToken = "" }, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _authService.Verify(s => s.LogoutAsync("", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Me_WithValidClaim_Returns200()
    {
        var dto = SampleUser();
        _authService.Setup(s => s.GetCurrentUserAsync(dto.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Success(dto));

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, dto.Id.ToString())
        }, "jwt"));

        var result = await CreateController(user).Me(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        ((OkObjectResult)result).Value.Should().Be(dto);
    }

    [Fact]
    public async Task Me_WithNoClaim_Returns401()
    {
        var result = await CreateController().Me(CancellationToken.None);
        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Me_WithNonGuidClaim_Returns401()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "not-a-guid")
        }, "jwt"));

        var result = await CreateController(user).Me(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Me_WhenServiceReturnsNotFound_Returns401()
    {
        var userId = Guid.NewGuid();
        _authService.Setup(s => s.GetCurrentUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<UserDto>.Failure(ResultError.NotFound, "x"));

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }, "jwt"));

        var result = await CreateController(user).Me(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }
}
