using AuthSystem.Domain.Entities;
using AuthSystem.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthSystem.UnitTests.Infrastructure.Persistence;

public class UserRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly UserRepository _repo;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _repo = new UserRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    private async Task<User> SeedAsync(string username = "alice01")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            PasswordHash = "hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetByUsernameAsync_Found_ReturnsUser()
    {
        var seeded = await SeedAsync();

        var result = await _repo.GetByUsernameAsync("alice01");

        result.Should().NotBeNull();
        result!.Id.Should().Be(seeded.Id);
    }

    [Fact]
    public async Task GetByUsernameAsync_CaseInsensitive()
    {
        await SeedAsync("Alice01");

        var result = await _repo.GetByUsernameAsync("ALICE01");

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_NotFound_ReturnsNull()
    {
        var result = await _repo.GetByUsernameAsync("ghost");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Found_ReturnsUser()
    {
        var seeded = await SeedAsync();
        (await _repo.GetByIdAsync(seeded.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        (await _repo.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task UsernameExistsAsync_Exists_ReturnsTrue()
    {
        await SeedAsync();
        (await _repo.UsernameExistsAsync("alice01")).Should().BeTrue();
    }

    [Fact]
    public async Task UsernameExistsAsync_CaseInsensitive()
    {
        await SeedAsync("Alice01");
        (await _repo.UsernameExistsAsync("alice01")).Should().BeTrue();
    }

    [Fact]
    public async Task UsernameExistsAsync_NotExists_ReturnsFalse()
    {
        (await _repo.UsernameExistsAsync("ghost")).Should().BeFalse();
    }

    [Fact]
    public async Task AddAsync_And_SaveChanges_Persist()
    {
        var u = new User
        {
            Id = Guid.NewGuid(),
            Username = "bob",
            PasswordHash = "hash",
        };

        await _repo.AddAsync(u);
        await _repo.SaveChangesAsync();

        (await _db.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_And_SaveChanges_PersistChanges()
    {
        var user = await SeedAsync();
        user.FailedLoginCount = 3;

        await _repo.UpdateAsync(user);
        await _repo.SaveChangesAsync();

        var fetched = await _db.Users.FirstAsync(u => u.Id == user.Id);
        fetched.FailedLoginCount.Should().Be(3);
    }
}
