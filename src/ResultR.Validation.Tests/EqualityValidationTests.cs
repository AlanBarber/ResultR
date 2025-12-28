namespace ResultR.Validation.Tests;

public class EqualityValidationTests
{
    [Fact]
    public async Task Equal_WithDifferentValue_ReturnsFailure()
    {
        var model = new TestModel { Status = "Active" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Status)
            .Equal("Pending")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Contains("must equal Pending", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Equal_WithSameValue_ReturnsSuccess()
    {
        var model = new TestModel { Status = "Active" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Status)
            .Equal("Active")
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Equal_WithNumericValue_Works()
    {
        var model = new TestModel { Count = 5 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Count)
            .Equal(5)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Equal_WithCustomMessage_UsesCustomMessage()
    {
        var model = new TestModel { Status = "Active" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Status)
            .Equal("Pending", "Status must be Pending")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Equal("Status must be Pending", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task NotEqual_WithSameValue_ReturnsFailure()
    {
        var model = new TestModel { Status = "Deleted" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Status)
            .NotEqual("Deleted")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Contains("must not equal Deleted", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task NotEqual_WithDifferentValue_ReturnsSuccess()
    {
        var model = new TestModel { Status = "Active" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Status)
            .NotEqual("Deleted")
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task NotEqual_WithNumericValue_Works()
    {
        var model = new TestModel { Count = 5 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Count)
            .NotEqual(0)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task NotEqual_WithCustomMessage_UsesCustomMessage()
    {
        var model = new TestModel { Status = "Deleted" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Status)
            .NotEqual("Deleted", "Cannot be deleted")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Equal("Cannot be deleted", errors[0].ErrorMessage);
    }

    private class TestModel
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
