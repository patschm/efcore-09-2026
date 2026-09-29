namespace WebShop.SharedKernel;

// A tiny, generic (non-domain) primitive every bounded context is free to depend on -
// unlike a real Shared Kernel, nothing here carries business meaning, so sharing it
// doesn't couple Catalog/Pricing/Reviews to each other's domain rules.
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }

    protected Result(bool isSuccess, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result Success() => new(true, Array.Empty<string>());

    public static Result Failure(params string[] errors) => new(false, errors);

    public static Result Failure(IReadOnlyList<string> errors) => new(false, errors);

    // Carries this result's success/failure onto a value once the caller has one ready,
    // so "if failed, propagate; otherwise wrap the value" doesn't repeat at every call site.
    public Result<T> Map<T>(T value) =>
        IsSuccess ? Result<T>.Success(value) : Result<T>.Failure(Errors);
}

public class Result<T>
{
    private readonly T? _value;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<string> Errors { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed result.");

    private Result(T? value, bool isSuccess, IReadOnlyList<string> errors)
    {
        _value = value;
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(value, true, Array.Empty<string>());

    public static Result<T> Failure(params string[] errors) => new(default, false, errors);

    public static Result<T> Failure(IReadOnlyList<string> errors) => new(default, false, errors);
}
