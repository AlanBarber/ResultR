namespace ResultR.Validation.Tests;

public class ValidatorTests
{
    [Fact]
    public void For_WithValidInstance_ReturnsValidationBuilder()
    {
        var instance = new TestModel { Name = "Test" };

        var builder = Validator.For(instance);

        Assert.NotNull(builder);
        Assert.Equal(instance, builder.Instance);
    }

    [Fact]
    public void For_WithNullInstance_ThrowsArgumentNullException()
    {
        TestModel? instance = null;

        var exception = Assert.Throws<ArgumentNullException>(() => Validator.For(instance!));

        Assert.Equal("instance", exception.ParamName);
    }

    private class TestModel
    {
        public string? Name { get; set; }
    }
}
