namespace ResultR;

/// <summary>
/// Represents the outcome of an operation that does not return a value.
/// Supports success/failure states, error messages, exception capture, and optional metadata.
/// </summary>
public class Result
{
    private Dictionary<string, object>? _metadata;

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error message if the operation failed; otherwise, null.
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets the exception if one was captured during the operation; otherwise, null.
    /// </summary>
    public Exception? Exception { get; }

    // Cached empty dictionary to avoid allocations when Metadata is accessed without any metadata set
    private static readonly IReadOnlyDictionary<string, object> EmptyMetadata = new Dictionary<string, object>();

    /// <summary>
    /// Gets the metadata dictionary. Returns an empty dictionary if no metadata has been set.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata => _metadata ?? EmptyMetadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    protected Result(bool isSuccess, string? error, Exception? exception)
    {
        IsSuccess = isSuccess;
        Error = error;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, null, null);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    public static Result Failure(string error) => new(false, error, null);

    /// <summary>
    /// Creates a failed result with an error message and exception.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    public static Result Failure(string error, Exception exception) => new(false, error, exception);

    /// <summary>
    /// Adds metadata to the result.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The current result instance for method chaining.</returns>
    public Result WithMetadata(string key, object value)
    {
        _metadata ??= [];
        _metadata[key] = value;
        return this;
    }
}

/// <summary>
/// Represents the outcome of an operation that returns a value of type <typeparamref name="T"/>.
/// Supports success/failure states, error messages, exception capture, and optional metadata.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Gets the value if the operation succeeded.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Value on a failed result.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value on a failed result. Error: {Error}");

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    private Result(bool isSuccess, T? value, string? error, Exception? exception)
        : base(isSuccess, error, exception)
    {
        _value = value;
    }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value to wrap in the result.</param>
    public static Result<T> Success(T value) => new(true, value, null, null);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    public new static Result<T> Failure(string error) => new(false, default, error, null);

    /// <summary>
    /// Creates a failed result with an error message and exception.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    public new static Result<T> Failure(string error, Exception exception) => new(false, default, error, exception);

    /// <summary>
    /// Adds metadata to the result.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The current result instance for method chaining.</returns>
    public new Result<T> WithMetadata(string key, object value)
    {
        base.WithMetadata(key, value);
        return this;
    }

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}
