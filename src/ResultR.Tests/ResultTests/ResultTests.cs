namespace ResultR.Tests.ResultTests;

/// <summary>
/// Tests for the non-generic Result type.
/// </summary>
public class ResultTests
{
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void Failure_WithMessage_CreatesFailedResult()
    {
        var result = Result.Failure("Something went wrong");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong", result.Error);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void Failure_WithMessageAndException_CreatesFailedResult()
    {
        var exception = new InvalidOperationException("Test exception");
        var result = Result.Failure("Operation failed", exception);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("Operation failed", result.Error);
        Assert.Same(exception, result.Exception);
    }

    [Fact]
    public void WithMetadata_AddsMetadataToResult()
    {
        var result = Result.Success()
            .WithMetadata("Key1", "Value1")
            .WithMetadata("Key2", 42);

        Assert.Equal("Value1", result.Metadata["Key1"]);
        Assert.Equal(42, result.Metadata["Key2"]);
    }

    [Fact]
    public void Metadata_ReturnsEmptyDictionary_WhenNoMetadataSet()
    {
        var result = Result.Success();

        Assert.Empty(result.Metadata);
    }
}
