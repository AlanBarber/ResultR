namespace ResultR.Validation.Tests;

public class CustomValidationTests
{
    [Fact]
    public async Task Must_WithFailingPredicate_ReturnsFailure()
    {
        var model = new TestModel { Value = 5 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Value)
            .Must(x => x % 2 == 0, "Value must be even")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Equal("Value must be even", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Must_WithPassingPredicate_ReturnsSuccess()
    {
        var model = new TestModel { Value = 4 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Value)
            .Must(x => x % 2 == 0, "Value must be even")
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Must_WithComplexPredicate_Works()
    {
        var model = new TestModel { Name = "Test123" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .Must(x => x != null && x.Any(char.IsDigit), "Name must contain at least one digit")
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Must_WithNullValue_CanHandleNull()
    {
        var model = new TestModel { Name = null };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .Must(x => x != null && x.Length > 0, "Name is required")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Equal("Name is required", errors[0].ErrorMessage);
    }

    private class TestModel
    {
        public int Value { get; set; }
        public string? Name { get; set; }
    }
}
