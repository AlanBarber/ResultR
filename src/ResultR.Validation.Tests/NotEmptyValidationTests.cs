namespace ResultR.Validation.Tests;

public class NotEmptyValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NotEmpty_String_WithEmptyValue_ReturnsFailure(string? value)
    {
        var model = new TestModel { Name = value };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .NotEmpty()
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Equal("Name", errors[0].PropertyName);
        Assert.Equal("Name is required.", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task NotEmpty_String_WithValidValue_ReturnsSuccess()
    {
        var model = new TestModel { Name = "Valid" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .NotEmpty()
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task NotEmpty_Collection_WithNullCollection_ReturnsFailure()
    {
        var model = new TestModelWithNullableItems { Items = null };

        var result = await Validator.For(model)
            .RuleFor(x => x.Items)
            .Must(x => x != null && x.Any(), "Items is required.")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Equal("Items", errors[0].PropertyName);
    }

    [Fact]
    public async Task NotEmpty_Collection_WithEmptyCollection_ReturnsFailure()
    {
        var model = new TestModel { Items = [] };

        var result = await Validator.For(model)
            .RuleFor(x => x.Items)
            .NotEmpty()
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Equal("Items", errors[0].PropertyName);
    }

    [Fact]
    public async Task NotEmpty_Collection_WithValidCollection_ReturnsSuccess()
    {
        var model = new TestModel { Items = [1, 2, 3] };

        var result = await Validator.For(model)
            .RuleFor(x => x.Items)
            .NotEmpty()
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task NotEmpty_WithCustomMessage_UsesCustomMessage()
    {
        var model = new TestModel { Name = "" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .NotEmpty("Custom error")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Equal("Custom error", errors[0].ErrorMessage);
    }

    private class TestModel
    {
        public string? Name { get; set; }
        public IEnumerable<int> Items { get; set; } = [];
    }

    private class TestModelWithNullableItems
    {
        public IEnumerable<int>? Items { get; set; }
    }
}
