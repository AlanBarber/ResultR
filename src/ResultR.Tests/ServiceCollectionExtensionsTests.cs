using Microsoft.Extensions.DependencyInjection;

namespace ResultR.Tests;

public class ServiceCollectionExtensionsTests
{
    public record SampleRequest(string Data) : IRequest<string>;

    public class SampleHandler : IRequestHandler<SampleRequest, string>
    {
        public ValueTask<Result<string>> HandleAsync(SampleRequest request, CancellationToken cancellationToken)
        {
            return new(Result<string>.Success(request.Data.ToUpperInvariant()));
        }
    }

    [Fact]
    public void AddResultR_RegistersMediator()
    {
        var services = new ServiceCollection();

        services.AddResultR(typeof(ServiceCollectionExtensionsTests).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>();

        Assert.NotNull(mediator);
        Assert.IsType<Mediator>(mediator);
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
    public async Task AddResultR_RegisteredHandlers_WorkWithMediator()
    {
        var services = new ServiceCollection();
        services.AddResultR(typeof(ServiceCollectionExtensionsTests).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new SampleRequest("hello"));

        Assert.True(result.IsSuccess);
        Assert.Equal("HELLO", result.Value);
    }
}
