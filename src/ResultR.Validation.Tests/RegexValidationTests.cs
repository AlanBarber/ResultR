using System.Text.RegularExpressions;

namespace ResultR.Validation.Tests;

public class RegexValidationTests
{
    [Fact]
    public async Task Matches_WithNonMatchingValue_ReturnsFailure()
    {
        var model = new TestModel { Code = "ABC" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Code)
            .Matches(@"^\d{3}$")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Equal("Code", errors[0].PropertyName);
        Assert.Contains("invalid", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task Matches_WithMatchingValue_ReturnsSuccess()
    {
        var model = new TestModel { Code = "123" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Code)
            .Matches(@"^\d{3}$")
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Matches_WithNullValue_ReturnsSuccess()
    {
        var model = new TestModel { Code = null };

        var result = await Validator.For(model)
            .RuleFor(x => x.Code)
            .Matches(@"^\d{3}$")
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Matches_WithRegexOptions_AppliesOptions()
    {
        var model = new TestModel { Code = "ABC" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Code)
            .Matches(@"^abc$", options: RegexOptions.IgnoreCase)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Matches_WithCustomMessage_UsesCustomMessage()
    {
        var model = new TestModel { Code = "ABC" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Code)
            .Matches(@"^\d{3}$", "Must be 3 digits")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Equal("Must be 3 digits", errors[0].ErrorMessage);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("test+tag@example.com")]
    public async Task EmailAddress_WithValidEmail_ReturnsSuccess(string email)
    {
        var model = new TestModel { Email = email };

        var result = await Validator.For(model)
            .RuleFor(x => x.Email)
            .EmailAddress()
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test @example.com")]
    public async Task EmailAddress_WithInvalidEmail_ReturnsFailure(string email)
    {
        var model = new TestModel { Email = email };

        var result = await Validator.For(model)
            .RuleFor(x => x.Email)
            .EmailAddress()
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Contains("valid email", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task EmailAddress_WithNullValue_ReturnsSuccess()
    {
        var model = new TestModel { Email = null };

        var result = await Validator.For(model)
            .RuleFor(x => x.Email)
            .EmailAddress()
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EmailAddress_WithCustomMessage_UsesCustomMessage()
    {
        var model = new TestModel { Email = "invalid" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Email)
            .EmailAddress("Invalid email format")
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Equal("Invalid email format", errors[0].ErrorMessage);
    }

    private class TestModel
    {
        public string? Code { get; set; }
        public string? Email { get; set; }
    }
}
