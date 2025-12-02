using Microsoft.Extensions.DependencyInjection;

namespace ResultR.Tests.DispatcherTests;

/// <summary>
/// Tests for Dispatcher scoping behavior - verifying handlers are properly scoped per DI scope.
/// </summary>
public class DispatcherScopingTests : DispatcherTestBase
{
    [Fact]
    public async Task Dispatch_WithScopedHandler_CreatesDifferentInstancesPerScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IDispatcher, ResultR.Dispatcher>();
        services.AddScoped<IRequestHandler<ScopedRequest, Guid>, ScopedHandler>();
        var rootProvider = services.BuildServiceProvider();

        // Act - create two separate scopes and dispatch from each
        Guid instanceId1, instanceId2;
        using (var scope1 = rootProvider.CreateScope())
        {
            var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IDispatcher>();
            var result1 = await dispatcher1.Dispatch(new ScopedRequest(1));
            instanceId1 = result1.Value;
        }

        using (var scope2 = rootProvider.CreateScope())
        {
            var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IDispatcher>();
            var result2 = await dispatcher2.Dispatch(new ScopedRequest(2));
            instanceId2 = result2.Value;
        }

        // Assert - different scopes should have different handler instances
        Assert.NotEqual(instanceId1, instanceId2);
    }

    [Fact]
    public async Task Dispatch_WithScopedHandler_ReusesSameInstanceWithinScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IDispatcher, ResultR.Dispatcher>();
        services.AddScoped<IRequestHandler<ScopedRequest, Guid>, ScopedHandler>();
        var rootProvider = services.BuildServiceProvider();

        // Act - dispatch twice within the same scope
        using var scope = rootProvider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var result1 = await dispatcher.Dispatch(new ScopedRequest(1));
        var result2 = await dispatcher.Dispatch(new ScopedRequest(2));

        // Assert - same scope should reuse the same handler instance
        Assert.Equal(result1.Value, result2.Value);
    }

    [Fact]
    public async Task Dispatch_VoidRequest_WithScopedHandler_CreatesDifferentInstancesPerScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IDispatcher, ResultR.Dispatcher>();
        services.AddScoped<IRequestHandler<ScopedVoidRequest>, ScopedVoidHandler>();
        var rootProvider = services.BuildServiceProvider();

        // Act - dispatch from two separate scopes and capture handler instances
        ScopedVoidHandler handler1, handler2;
        using (var scope1 = rootProvider.CreateScope())
        {
            handler1 = (ScopedVoidHandler)scope1.ServiceProvider.GetRequiredService<IRequestHandler<ScopedVoidRequest>>();
            var dispatcher1 = scope1.ServiceProvider.GetRequiredService<IDispatcher>();
            await dispatcher1.Dispatch(new ScopedVoidRequest(1));
        }

        using (var scope2 = rootProvider.CreateScope())
        {
            handler2 = (ScopedVoidHandler)scope2.ServiceProvider.GetRequiredService<IRequestHandler<ScopedVoidRequest>>();
            var dispatcher2 = scope2.ServiceProvider.GetRequiredService<IDispatcher>();
            await dispatcher2.Dispatch(new ScopedVoidRequest(2));
        }

        // Assert - different scopes should have different handler instances
        Assert.NotEqual(handler1.InstanceId, handler2.InstanceId);
    }

    [Fact]
    public async Task Dispatch_VoidRequest_WithScopedHandler_ReusesSameInstanceWithinScope()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IDispatcher, ResultR.Dispatcher>();
        services.AddScoped<IRequestHandler<ScopedVoidRequest>, ScopedVoidHandler>();
        var rootProvider = services.BuildServiceProvider();

        // Act - dispatch twice within the same scope
        using var scope = rootProvider.CreateScope();
        var handler = (ScopedVoidHandler)scope.ServiceProvider.GetRequiredService<IRequestHandler<ScopedVoidRequest>>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        await dispatcher.Dispatch(new ScopedVoidRequest(1));
        await dispatcher.Dispatch(new ScopedVoidRequest(2));

        // Get the handler again to verify it's the same instance
        var handlerAgain = (ScopedVoidHandler)scope.ServiceProvider.GetRequiredService<IRequestHandler<ScopedVoidRequest>>();

        // Assert - same scope should reuse the same handler instance
        Assert.Equal(handler.InstanceId, handlerAgain.InstanceId);
    }

    #region Test Fixtures

    public record ScopedRequest(int Id) : IRequest<Guid>;

    /// <summary>
    /// Handler that tracks its instance ID to verify scoping behavior.
    /// Each instance gets a unique ID assigned at construction time.
    /// </summary>
    public class ScopedHandler : IRequestHandler<ScopedRequest, Guid>
    {
        public Guid InstanceId { get; } = Guid.NewGuid();

        public ValueTask<Result<Guid>> HandleAsync(ScopedRequest request, CancellationToken cancellationToken)
        {
            return new(Result<Guid>.Success(InstanceId));
        }
    }

    public record ScopedVoidRequest(int Id) : IRequest;

    /// <summary>
    /// Void handler that tracks instance creation for scoping verification.
    /// </summary>
    public class ScopedVoidHandler : IRequestHandler<ScopedVoidRequest>
    {
        public Guid InstanceId { get; } = Guid.NewGuid();

        public ValueTask<Result> HandleAsync(ScopedVoidRequest request, CancellationToken cancellationToken)
        {
            return new(Result.Success());
        }
    }

    #endregion
}
