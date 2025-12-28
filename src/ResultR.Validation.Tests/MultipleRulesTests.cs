namespace ResultR.Validation.Tests;

public class MultipleRulesTests
{
    [Fact]
    public async Task MultipleRules_OnSameProperty_AllValidated()
    {
        var model = new TestModel { Name = "ab" };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .NotEmpty()
            .MinLength(3)
            .MaxLength(10)
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Single(errors);
        Assert.Contains("at least 3 characters", errors[0].ErrorMessage);
    }

    [Fact]
    public async Task MultipleRules_OnDifferentProperties_AllValidated()
    {
        var model = new TestModel { Name = "", Age = 5 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .NotEmpty()
            .RuleFor(x => x.Age)
            .GreaterThan(10)
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, e => e.PropertyName == "Name");
        Assert.Contains(errors, e => e.PropertyName == "Age");
    }

    [Fact]
    public async Task MultipleRules_AllPass_ReturnsSuccess()
    {
        var model = new TestModel { Name = "Valid", Age = 25 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .NotEmpty()
            .MinLength(3)
            .RuleFor(x => x.Age)
            .GreaterThan(10)
            .LessThan(100)
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ChainedRules_WithMultipleFailures_ReturnsAllErrors()
    {
        var model = new TestModel { Name = "a", Age = 150 };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .MinLength(3)
            .MaxLength(10)
            .RuleFor(x => x.Age)
            .GreaterThan(0)
            .LessThan(120)
            .ToResult();

        Assert.False(result.IsSuccess);
        var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
        Assert.NotNull(errors);
        Assert.Equal(2, errors.Count);
    }

    [Fact]
    public async Task ComplexValidation_WithNestedProperties_Works()
    {
        var model = new TestModel
        {
            Name = "Test",
            Age = 25,
            Email = "test@example.com"
        };

        var result = await Validator.For(model)
            .RuleFor(x => x.Name)
            .NotEmpty()
            .MinLength(2)
            .MaxLength(50)
            .RuleFor(x => x.Age)
            .GreaterThan(0)
            .LessThan(150)
            .RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .ToResult();

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidationBuilder_CanBeReused_ForMultipleValidations()
    {
        var model1 = new TestModel { Name = "Valid", Age = 25 };
        var model2 = new TestModel { Name = "", Age = 5 };

        var result1 = await Validator.For(model1)
            .RuleFor(x => x.Name)
            .NotEmpty()
            .RuleFor(x => x.Age)
            .GreaterThan(10)
            .ToResult();

        var result2 = await Validator.For(model2)
            .RuleFor(x => x.Name)
            .NotEmpty()
            .RuleFor(x => x.Age)
            .GreaterThan(10)
            .ToResult();

        Assert.True(result1.IsSuccess);
        Assert.False(result2.IsSuccess);
    }

    private class TestModel
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string? Email { get; set; }
    }
}
