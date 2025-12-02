using Microsoft.Extensions.DependencyInjection;

namespace ResultR.Tests.ServiceCollectionExtensionsTests;

/// <summary>
/// Tests for the ServiceCollectionExtensions DI registration methods.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddResultR_RegistersDispatcher()
    {
        var services = new ServiceCollection();

        services.AddResultR(typeof(ServiceCollectionExtensionsTests).Assembly);

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetService<IDispatcher>();

        Assert.NotNull(dispatcher);
        Assert.IsType<Dispatcher>(dispatcher);
    }

    [Fact]
    public void AddResultR_RegistersHandlersFromAssembly()
    {
        var services = new ServiceCollection();

        services.AddResultR(typeof(ServiceCollectionExtensionsTests).Assembly);

        var provider = services.BuildServiceProvider();
        var handler = provider.GetService<IRequestHandler<SampleRequest, string>>();

        Assert.NotNull(handler);
        Assert.IsType<SampleHandler>(handler);
    }

    [Fact]
    public void AddResultR_WithNullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddResultR(typeof(ServiceCollectionExtensionsTests).Assembly));
    }

    [Fact]
    public void AddResultR_WithEmptyAssemblyArray_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddResultR(Array.Empty<System.Reflection.Assembly>()));
    }

    [Fact]
    public async Task AddResultR_RegisteredHandlers_WorkWithDispatcher()
    {
        var services = new ServiceCollection();
        services.AddResultR(typeof(ServiceCollectionExtensionsTests).Assembly);
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        var result = await dispatcher.Dispatch(new SampleRequest("hello"));

        Assert.True(result.IsSuccess);
        Assert.Equal("HELLO", result.Value);
    }

    [Fact]
    public void AddResultR_RegistersVoidHandlersFromAssembly()
    {
        var services = new ServiceCollection();

        services.AddResultR(typeof(ServiceCollectionExtensionsTests).Assembly);

        var provider = services.BuildServiceProvider();
        var handler = provider.GetService<IRequestHandler<SampleVoidRequest>>();

        Assert.NotNull(handler);
        Assert.IsType<SampleVoidHandler>(handler);
    }

    [Fact]
    public async Task AddResultR_RegisteredVoidHandlers_WorkWithDispatcher()
    {
        var services = new ServiceCollection();
        services.AddResultR(typeof(ServiceCollectionExtensionsTests).Assembly);
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        var result = await dispatcher.Dispatch(new SampleVoidRequest(42));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AddResultR_WithMultipleAssemblies_RegistersHandlersFromAll()
    {
        var services = new ServiceCollection();
        var testAssembly = typeof(ServiceCollectionExtensionsTests).Assembly;
        var resultRAssembly = typeof(Dispatcher).Assembly;

        // Register from multiple assemblies (ResultR assembly has no handlers, but this tests the multi-assembly path)
        services.AddResultR(testAssembly, resultRAssembly);

        var provider = services.BuildServiceProvider();

        // Handlers from test assembly should be registered
        var handler = provider.GetService<IRequestHandler<SampleRequest, string>>();
        var voidHandler = provider.GetService<IRequestHandler<SampleVoidRequest>>();

        Assert.NotNull(handler);
        Assert.NotNull(voidHandler);
    }

    #region Test Fixtures

    public record SampleRequest(string Data) : IRequest<string>;

    public class SampleHandler : IRequestHandler<SampleRequest, string>
    {
        public ValueTask<Result<string>> HandleAsync(SampleRequest request, CancellationToken cancellationToken)
        {
            return new(Result<string>.Success(request.Data.ToUpperInvariant()));
        }
    }

    public record SampleVoidRequest(int Id) : IRequest;

    public class SampleVoidHandler : IRequestHandler<SampleVoidRequest>
    {
        public ValueTask<Result> HandleAsync(SampleVoidRequest request, CancellationToken cancellationToken)
        {
            return new(Result.Success());
        }
    }

    #endregion
}
