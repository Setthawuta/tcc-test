using AuthSystem.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace AuthSystem.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void NewUser_HasDefaultValues()
    {
        var user = new User();

        user.Id.Should().NotBe(Guid.Empty);
        user.Username.Should().BeEmpty();
        user.PasswordHash.Should().BeEmpty();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.LastLoginAt.Should().BeNull();
        user.FailedLoginCount.Should().Be(0);
        user.LockedUntil.Should().BeNull();
    }

    [Fact]
    public void IsLocked_WhenLockedUntilNull_ReturnsFalse()
    {
        var user = new User { LockedUntil = null };
        user.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void IsLocked_WhenLockedUntilInPast_ReturnsFalse()
    {
        var user = new User { LockedUntil = DateTime.UtcNow.AddMinutes(-1) };
        user.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void IsLocked_WhenLockedUntilInFuture_ReturnsTrue()
    {
        var user = new User { LockedUntil = DateTime.UtcNow.AddMinutes(5) };
        user.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void RecordSuccessfulLogin_ResetsFailureAndLock_SetsLastLogin()
    {
        var user = new User
        {
            FailedLoginCount = 4,
            LockedUntil = DateTime.UtcNow.AddMinutes(10),
            UpdatedAt = DateTime.UtcNow.AddHours(-1),
        };

        user.RecordSuccessfulLogin();

        user.FailedLoginCount.Should().Be(0);
        user.LockedUntil.Should().BeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordFailedLogin_IncrementsCountUnderThreshold_DoesNotLock()
    {
        var user = new User { FailedLoginCount = 2 };

        user.RecordFailedLogin();

        user.FailedLoginCount.Should().Be(3);
        user.LockedUntil.Should().BeNull();
        user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RecordFailedLogin_WhenReachesFive_SetsLockedUntilFiveMinutesOut()
    {
        var user = new User { FailedLoginCount = 4 };

        user.RecordFailedLogin();

        user.FailedLoginCount.Should().Be(5);
        user.LockedUntil.Should().NotBeNull();
        user.LockedUntil!.Value.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordFailedLogin_BeyondThreshold_KeepsLockExtended()
    {
        var user = new User { FailedLoginCount = 10 };

        user.RecordFailedLogin();

        user.FailedLoginCount.Should().Be(11);
        user.LockedUntil.Should().NotBeNull();
    }
}
