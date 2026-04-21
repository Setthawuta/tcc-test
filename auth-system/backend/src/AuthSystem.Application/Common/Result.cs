namespace AuthSystem.Application.Common;

public enum ResultError
{
    None,
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Locked,
    Unknown
}

public class Result
{
    public bool IsSuccess { get; }
    public ResultError ErrorType { get; }
    public string? Error { get; }

    protected Result(bool isSuccess, ResultError errorType, string? error)
    {
        IsSuccess = isSuccess;
        ErrorType = errorType;
        Error = error;
    }

    public static Result Success() => new(true, ResultError.None, null);
    public static Result Failure(ResultError errorType, string error) => new(false, errorType, error);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, ResultError.None, null)
    {
        Value = value;
    }

    private Result(ResultError errorType, string error) : base(false, errorType, error)
    {
        Value = default;
    }

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(ResultError errorType, string error) => new(errorType, error);
}
