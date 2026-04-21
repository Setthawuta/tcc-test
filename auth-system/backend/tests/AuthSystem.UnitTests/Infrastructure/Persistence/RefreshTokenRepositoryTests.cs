using AuthSystem.Domain.Entities;
using AuthSystem.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthSystem.UnitTests.Infrastructure.Persistence;

public class RefreshTokenRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly RefreshTokenRepository _repo;

    public RefreshTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repo = new RefreshTokenRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<RefreshToken> SeedAsync(string hash, Guid? userId = null, DateTime? expiresAt = null, DateTime? revokedAt = null)
    {
        var t = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            TokenHash = hash,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow,
            RevokedAt = revokedAt,
        };
        _db.RefreshTokens.Add(t);
        await _db.SaveChangesAsync();
        return t;
    }

    [Fact]
    public async Task GetByHashAsync_Found_ReturnsToken()
    {
        var t = await SeedAsync("abc");
        (await _repo.GetByHashAsync("abc"))!.Id.Should().Be(t.Id);
    }

    [Fact]
    public async Task GetByHashAsync_NotFound_ReturnsNull()
    {
        (await _repo.GetByHashAsync("absent")).Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_And_SaveChanges_Persist()
    {
        var t = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TokenHash = "new",
            ExpiresAt = DateTime.UtcNow.AddDays(1),
        };

        await _repo.AddAsync(t);
        await _repo.SaveChangesAsync();

        (await _db.RefreshTokens.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_SaveChanges_PersistRevocation()
    {
        var t = await SeedAsync("abc");
        t.Revoke();

        await _repo.UpdateAsync(t);
        await _repo.SaveChangesAsync();

        (await _db.RefreshTokens.FirstAsync(x => x.Id == t.Id)).RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeAllForUserAsync_RevokesOnlyActiveTokensForUser()
    {
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var activeA = await SeedAsync("a1", userA);
        var alreadyRevokedA = await SeedAsync("a2", userA, revokedAt: DateTime.UtcNow.AddMinutes(-1));
        var anotherUser = await SeedAsync("b1", userB);

        await _repo.RevokeAllForUserAsync(userA);
        await _repo.SaveChangesAsync();

        (await _db.RefreshTokens.FirstAsync(x => x.Id == activeA.Id)).RevokedAt.Should().NotBeNull();
        (await _db.RefreshTokens.FirstAsync(x => x.Id == alreadyRevokedA.Id)).RevokedAt!.Value
            .Should().BeCloseTo(alreadyRevokedA.RevokedAt!.Value, TimeSpan.FromSeconds(1));
        (await _db.RefreshTokens.FirstAsync(x => x.Id == anotherUser.Id)).RevokedAt.Should().BeNull();
    }
}
