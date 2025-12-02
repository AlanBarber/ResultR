namespace ResultR.Tests.ResultTests;

/// <summary>
/// Tests for the generic Result<T> type.
/// </summary>
public class ResultOfTTests
{
    [Fact]
    public void Success_CreatesSuccessfulResultWithValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void Failure_WithMessage_CreatesFailedResult()
    {
        var result = Result<int>.Failure("Not found");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Not found", result.Error);
    }

    [Fact]
    public void Failure_WithMessageAndException_CreatesFailedResult()
    {
        var exception = new ArgumentException("Invalid argument");
        var result = Result<string>.Failure("Validation failed", exception);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Validation failed", result.Error);
        Assert.Same(exception, result.Exception);
    }

    [Fact]
    public void Value_ThrowsException_WhenResultIsFailed()
    {
        var result = Result<int>.Failure("Error occurred");

        var exception = Assert.Throws<InvalidOperationException>(() => _ = result.Value);
        Assert.Contains("Error occurred", exception.Message);
    }

    [Fact]
    public void WithMetadata_AddsMetadataToResult()
    {
        var result = Result<string>.Success("test")
            .WithMetadata("Timestamp", DateTime.UtcNow)
            .WithMetadata("Source", "API");

        Assert.Equal("API", result.Metadata["Source"]);
        Assert.True(result.Metadata.ContainsKey("Timestamp"));
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccessResult()
    {
        Result<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }
}
