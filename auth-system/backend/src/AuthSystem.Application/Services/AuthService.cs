using AuthSystem.Application.Common;
using AuthSystem.Application.DTOs;
using AuthSystem.Application.Interfaces;
using AuthSystem.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AuthSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenHasher refreshTokenHasher,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenHasher = refreshTokenHasher;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _logger = logger;
    }

    public async Task<Result<UserDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<UserDto>.Failure(ResultError.Validation, message);
        }

        var exists = await _userRepository.UsernameExistsAsync(request.Username, cancellationToken);
        if (exists)
        {
            _logger.LogInformation("Registration attempt for existing username");
            return Result<UserDto>.Failure(ResultError.Conflict, "Username already exists");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User registered successfully: {UserId}", user.Id);

        return Result<UserDto>.Success(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            CreatedAt = user.CreatedAt
        });
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthResponse>.Failure(ResultError.Unauthorized, "Invalid username or password");
        }

        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null)
        {
            _logger.LogInformation("Login attempt for unknown username");
            return Result<AuthResponse>.Failure(ResultError.Unauthorized, "Invalid username or password");
        }

        if (user.IsLocked)
        {
            _logger.LogWarning("Login attempt for locked account: {UserId}", user.Id);
            return Result<AuthResponse>.Failure(ResultError.Unauthorized, "Login attempt for locked account");
        }

        var passwordOk = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!passwordOk)
        {
            user.RecordFailedLogin();
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Failed login for user {UserId}", user.Id);
            return Result<AuthResponse>.Failure(ResultError.Unauthorized, "Invalid username or password");
        }

        user.RecordSuccessfulLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);

        var authResponse = await IssueTokensAsync(user, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User logged in: {UserId}", user.Id);
        return Result<AuthResponse>.Success(authResponse);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<AuthResponse>.Failure(ResultError.Unauthorized, "Invalid refresh token");
        }

        var hash = _refreshTokenHasher.Hash(refreshToken);
        var stored = await _refreshTokenRepository.GetByHashAsync(hash, cancellationToken);

        if (stored is null)
        {
            _logger.LogWarning("Refresh attempt with unknown token");
            return Result<AuthResponse>.Failure(ResultError.Unauthorized, "Invalid refresh token");
        }

        if (!stored.IsActive)
        {
            if (stored.RevokedAt is not null)
            {
                _logger.LogWarning("Reuse of revoked refresh token for user {UserId} — revoking all", stored.UserId);
                await _refreshTokenRepository.RevokeAllForUserAsync(stored.UserId, cancellationToken);
                await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
            }
            return Result<AuthResponse>.Failure(ResultError.Unauthorized, "Invalid refresh token");
        }

        var user = await _userRepository.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null || user.IsLocked)
        {
            return Result<AuthResponse>.Failure(ResultError.Unauthorized, "Invalid refresh token");
        }

        var newResponse = await IssueTokensAsync(user, cancellationToken);

        stored.Revoke(replacedByTokenId: await FindIdByRawTokenAsync(newResponse.RefreshToken, cancellationToken));
        await _refreshTokenRepository.UpdateAsync(stored, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);
        return Result<AuthResponse>.Success(newResponse);
    }

    public async Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Success();
        }

        var hash = _refreshTokenHasher.Hash(refreshToken);
        var stored = await _refreshTokenRepository.GetByHashAsync(hash, cancellationToken);
        if (stored is { RevokedAt: null })
        {
            stored.Revoke();
            await _refreshTokenRepository.UpdateAsync(stored, cancellationToken);
            await _refreshTokenRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User logged out: {UserId}", stored.UserId);
        }
        return Result.Success();
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<UserDto>.Failure(ResultError.NotFound, "User not found");
        }

        return Result<UserDto>.Success(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            CreatedAt = user.CreatedAt
        });
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var (access, expiresIn) = _jwtTokenService.GenerateAccessToken(user);
        var (rawRefresh, refreshExpiresAt) = _jwtTokenService.GenerateRefreshToken();

        var refreshEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = _refreshTokenHasher.Hash(rawRefresh),
            ExpiresAt = refreshExpiresAt,
            CreatedAt = DateTime.UtcNow,
        };

        await _refreshTokenRepository.AddAsync(refreshEntity, cancellationToken);

        return new AuthResponse
        {
            AccessToken = access,
            RefreshToken = rawRefresh,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            User = new UserDto { Id = user.Id, Username = user.Username, CreatedAt = user.CreatedAt }
        };
    }

    private async Task<Guid?> FindIdByRawTokenAsync(string rawToken, CancellationToken cancellationToken)
    {
        var hash = _refreshTokenHasher.Hash(rawToken);
        var token = await _refreshTokenRepository.GetByHashAsync(hash, cancellationToken);
        return token?.Id;
    }
}
