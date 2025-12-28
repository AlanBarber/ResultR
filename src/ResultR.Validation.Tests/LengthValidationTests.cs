namespace ResultR.Validation.Tests;

public class LengthValidationTests
{
    [Fact]
    public async Task MinLength_WithTooShortValue_ReturnsFailure()
    {
        var model = new TestModel { Name = "ab" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .MinLength(3)
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Equal("Name", errors[0].PropertyName);
        Assert.Contains("at least 3 characters", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task MinLength_WithValidValue_ReturnsSuccess()
    {
        var model = new TestModel { Name = "abc" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .MinLength(3)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task MinLength_WithNullValue_ReturnsSuccess()
    {
        var model = new TestModel { Name = null };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .MinLength(3)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task MaxLength_WithTooLongValue_ReturnsFailure()
    {
        var model = new TestModel { Name = "abcdef" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .MaxLength(5)
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Contains("5 characters or fewer", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task MaxLength_WithValidValue_ReturnsSuccess()
    {
        var model = new TestModel { Name = "abc" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .MaxLength(5)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task MaxLength_WithNullValue_ReturnsSuccess()
    {
        var model = new TestModel { Name = null };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .MaxLength(5)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("abcdef")]
    public async Task Length_WithOutOfRangeValue_ReturnsFailure(string value)
    {
        var model = new TestModel { Name = value };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .Length(3, 5)
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Contains("between 3 and 5 characters", errors[0].ErrorMessage);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("abcd")]
    [InlineData("abcde")]
    public async Task Length_WithValidValue_ReturnsSuccess(string value)
    {
        var model = new TestModel { Name = value };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .Length(3, 5)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Length_WithNullValue_ReturnsSuccess()
    {
        var model = new TestModel { Name = null };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .Length(3, 5)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task MinLength_WithCustomMessage_UsesCustomMessage()
    {
        var model = new TestModel { Name = "ab" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .MinLength(3, "Too short!")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Equal("Too short!", errors[0].ErrorMessage);
    }

    private class TestModel
    {
        public string? Name { get; set; }
    }
}
