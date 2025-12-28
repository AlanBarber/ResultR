namespace ResultR.Validation.Tests;

public class NotNullValidationTests
{
    [Fact]
    public async Task NotNull_WithNullValue_ReturnsFailure()
    {
        var model = new TestModel { Reference = null };

        var result = await Validator.For(model)
            .RuleFor(x => x.Reference)
            .NotNull()
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Equal("Reference", errors[0].PropertyName);
        Assert.Equal("Reference is required.", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task NotNull_WithNonNullValue_ReturnsSuccess()
    {
        var model = new TestModel { Reference = new TestReference() };

        var result = await Validator.For(model)
            .RuleFor(x => x.Reference)
            .NotNull()
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task NotNull_WithCustomMessage_UsesCustomMessage()
    {
        var model = new TestModel { Reference = null };

        var result = await Validator.For(model)
            .RuleFor(x => x.Reference)
            .NotNull("Custom error message")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Equal("Custom error message", errors[0].ErrorMessage);
    }

    private class TestModel
    {
        public string? Name { get; set; }
        public TestReference? Reference { get; set; }
    }

    private class TestReference
    {
        public string Value { get; set; } = string.Empty;
    }
}
