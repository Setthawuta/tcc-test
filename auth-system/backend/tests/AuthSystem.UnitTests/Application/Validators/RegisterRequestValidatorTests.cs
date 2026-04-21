using AuthSystem.Application.DTOs;
using AuthSystem.Application.Validators;
using FluentAssertions;
using Xunit;

namespace AuthSystem.UnitTests.Application.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest Valid() => new()
    {
        Username = "alice01",
        Password = "Passw0rd",
        ConfirmPassword = "Passw0rd",
    };

    [Fact]
    public void ValidInput_Passes()
    {
        var result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("a_b")]
    [InlineData("alice@work")]
    public void InvalidUsername_Fails(string username)
    {
        var req = Valid();
        req.Username = username;
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Username));
    }

    [Fact]
    public void Username_ExactlyMaxLength_Passes()
    {
        var req = Valid();
        req.Username = new string('a', 50);
        _validator.Validate(req).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Username_Over50Chars_Fails()
    {
        var req = Valid();
        req.Username = new string('a', 51);
        _validator.Validate(req).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short1A")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public void InvalidPassword_Fails(string password)
    {
        var req = Valid();
        req.Password = password;
        req.ConfirmPassword = password;
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void ConfirmPassword_Empty_Fails()
    {
        var req = Valid();
        req.ConfirmPassword = "";
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ConfirmPassword_Mismatch_Fails()
    {
        var req = Valid();
        req.ConfirmPassword = "Different1";
        var result = _validator.Validate(req);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.ConfirmPassword));
    }
}
