using AuthSystem.Application.DTOs;
using AuthSystem.Application.Validators;
using FluentAssertions;
using Xunit;

namespace AuthSystem.UnitTests.Application.Validators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void BothPresent_Passes()
    {
        var result = _validator.Validate(new LoginRequest { Username = "alice", Password = "x" });
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void MissingUsername_Fails()
    {
        var result = _validator.Validate(new LoginRequest { Username = "", Password = "x" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Username));
    }

    [Fact]
    public void MissingPassword_Fails()
    {
        var result = _validator.Validate(new LoginRequest { Username = "alice", Password = "" });
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    public void BothMissing_Fails()
    {
        var result = _validator.Validate(new LoginRequest());
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }
}
