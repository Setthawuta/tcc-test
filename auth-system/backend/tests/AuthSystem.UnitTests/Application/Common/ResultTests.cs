using AuthSystem.Application.Common;
using FluentAssertions;
using Xunit;

namespace AuthSystem.UnitTests.Application.Common;

public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessResult()
    {
        var r = Result.Success();
        r.IsSuccess.Should().BeTrue();
        r.ErrorType.Should().Be(ResultError.None);
        r.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_CreatesFailureResult()
    {
        var r = Result.Failure(ResultError.Validation, "bad input");
        r.IsSuccess.Should().BeFalse();
        r.ErrorType.Should().Be(ResultError.Validation);
        r.Error.Should().Be("bad input");
    }

    [Fact]
    public void Generic_Success_StoresValue()
    {
        var r = Result<string>.Success("hello");
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be("hello");
        r.ErrorType.Should().Be(ResultError.None);
    }

    [Fact]
    public void Generic_Failure_HasDefaultValue()
    {
        var r = Result<string>.Failure(ResultError.NotFound, "missing");
        r.IsSuccess.Should().BeFalse();
        r.ErrorType.Should().Be(ResultError.NotFound);
        r.Error.Should().Be("missing");
        r.Value.Should().BeNull();
    }

    [Theory]
    [InlineData(ResultError.Conflict)]
    [InlineData(ResultError.Unauthorized)]
    [InlineData(ResultError.Locked)]
    [InlineData(ResultError.Unknown)]
    public void Generic_Failure_PreservesErrorType(ResultError errorType)
    {
        var r = Result<int>.Failure(errorType, "err");
        r.ErrorType.Should().Be(errorType);
    }
}
