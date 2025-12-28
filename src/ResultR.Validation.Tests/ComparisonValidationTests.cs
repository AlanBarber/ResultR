namespace ResultR.Validation.Tests;

public class ComparisonValidationTests
{
    [Fact]
    public async Task GreaterThan_WithSmallerValue_ReturnsFailure()
    {
        var model = new TestModel { Age = 5 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .GreaterThan(10)
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Contains("greater than 10", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task GreaterThan_WithEqualValue_ReturnsFailure()
    {
        var model = new TestModel { Age = 10 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .GreaterThan(10)
            .ToResult();

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GreaterThan_WithLargerValue_ReturnsSuccess()
    {
        var model = new TestModel { Age = 15 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .GreaterThan(10)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GreaterThan_WithNullableValue_ReturnsSuccess()
    {
        var model = new TestModel { NullableAge = null };

        var result = await Validator.For(model)
            .RuleFor(x => x.NullableAge)
            .Must(x => x == null || x > 10, "NullableAge must be greater than 10")
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GreaterThanOrEqualTo_WithSmallerValue_ReturnsFailure()
    {
        var model = new TestModel { Age = 5 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(10)
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Contains("greater than or equal to 10", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task GreaterThanOrEqualTo_WithEqualValue_ReturnsSuccess()
    {
        var model = new TestModel { Age = 10 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(10)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GreaterThanOrEqualTo_WithLargerValue_ReturnsSuccess()
    {
        var model = new TestModel { Age = 15 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .GreaterThanOrEqualTo(10)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LessThan_WithLargerValue_ReturnsFailure()
    {
        var model = new TestModel { Age = 15 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .LessThan(10)
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Contains("less than 10", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task LessThan_WithEqualValue_ReturnsFailure()
    {
        var model = new TestModel { Age = 10 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .LessThan(10)
            .ToResult();

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task LessThan_WithSmallerValue_ReturnsSuccess()
    {
        var model = new TestModel { Age = 5 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .LessThan(10)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LessThanOrEqualTo_WithLargerValue_ReturnsFailure()
    {
        var model = new TestModel { Age = 15 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .LessThanOrEqualTo(10)
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Contains("less than or equal to 10", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task LessThanOrEqualTo_WithEqualValue_ReturnsSuccess()
    {
        var model = new TestModel { Age = 10 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .LessThanOrEqualTo(10)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task LessThanOrEqualTo_WithSmallerValue_ReturnsSuccess()
    {
        var model = new TestModel { Age = 5 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .LessThanOrEqualTo(10)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Between_WithValueBelowRange_ReturnsFailure()
    {
        var model = new TestModel { Age = 5 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .Between(10, 20)
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Contains("between 10 and 20", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Between_WithValueAboveRange_ReturnsFailure()
    {
        var model = new TestModel { Age = 25 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .Between(10, 20)
            .ToResult();

        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(20)]
    public async Task Between_WithValueInRange_ReturnsSuccess(int age)
    {
        var model = new TestModel { Age = age };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .Between(10, 20)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Between_WithNullableValue_ReturnsSuccess()
    {
        var model = new TestModel { NullableAge = null };

        var result = await Validator.For(model)
            .RuleFor(x => x.NullableAge)
            .Must(x => x == null || (x >= 10 && x <= 20), "NullableAge must be between 10 and 20")
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GreaterThan_WithCustomMessage_UsesCustomMessage()
    {
        var model = new TestModel { Age = 5 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Age)
            .GreaterThan(10, "Age too low")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Equal("Age too low", errors[0].ErrorMessage);
    }

    private class TestModel
    {
        public int Age { get; set; }
        public int? NullableAge { get; set; }
    }
}
