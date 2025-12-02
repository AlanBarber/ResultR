using Microsoft.Extensions.DependencyInjection;

namespace ResultR.Tests.DispatcherTests;

/// <summary>
/// Base class containing shared test fixtures and helper methods for Dispatcher tests.
/// </summary>
public abstract class DispatcherTestBase
{
    #region Helper Methods

    protected static IDispatcher CreateDispatcher(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        services.AddScoped<IDispatcher, ResultR.Dispatcher>();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IDispatcher>();
    }

    #endregion

    #region Request/Response Fixtures

    public record TestRequest(string Value) : IRequest<string>;

    public class TestHandler : IRequestHandler<TestRequest, string>
    {
        public ValueTask<Result<string>> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return new(Result<string>.Success($"Handled: {request.Value}"));
        }
    }

    public record ThrowingRequest(string Message) : IRequest<string>;

    public class ThrowingHandler : IRequestHandler<ThrowingRequest, string>
    {
        public ValueTask<Result<string>> HandleAsync(ThrowingRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(request.Message);
        }
    }

    #endregion

    #region Void Request Fixtures

    public record VoidRequest(int Id) : IRequest;

    public class VoidHandler : IRequestHandler<VoidRequest>
    {
        public bool HandleCalled { get; private set; }
        public int ReceivedId { get; private set; }

        public ValueTask<Result> HandleAsync(VoidRequest request, CancellationToken cancellationToken)
        {
            HandleCalled = true;
            ReceivedId = request.Id;
            return new(Result.Success());
        }
    }

    #endregion
}
