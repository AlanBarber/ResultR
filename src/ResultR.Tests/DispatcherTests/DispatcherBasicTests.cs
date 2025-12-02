using Microsoft.Extensions.DependencyInjection;

namespace ResultR.Tests.DispatcherTests;

/// <summary>
/// Tests for basic Dispatcher functionality with request/response handlers.
/// </summary>
public class DispatcherBasicTests : DispatcherTestBase
{
    [Fact]
    public async Task Dispatch_WithValidHandler_ReturnsSuccessResult()
    {
        var dispatcher = CreateDispatcher(s => s.AddScoped<IRequestHandler<TestRequest, string>, TestHandler>());
        var request = new TestRequest("Hello");

        var result = await dispatcher.Dispatch(request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Handled: Hello", result.Value);
    }

    [Fact]
    public async Task Dispatch_WithNoHandler_ThrowsException()
    {
        var dispatcher = CreateDispatcher(_ => { });
        var request = new TestRequest("Hello");

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.Dispatch(request));
    }

    [Fact]
    public async Task Dispatch_WithNullRequest_ThrowsArgumentNullException()
    {
        var dispatcher = CreateDispatcher(s => s.AddScoped<IRequestHandler<TestRequest, string>, TestHandler>());

        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.Dispatch<string>(null!));
    }

    [Fact]
    public async Task Dispatch_ReturnsFailureResult_WhenHandlerThrowsException()
    {
        var dispatcher = CreateDispatcher(s => s.AddScoped<IRequestHandler<ThrowingRequest, string>, ThrowingHandler>());
        var request = new ThrowingRequest("Something went wrong");

        var result = await dispatcher.Dispatch(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong", result.Error);
        Assert.NotNull(result.Exception);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    [Fact]
    public async Task Dispatch_ReturnsFailureResult_WhenHandlerReturnsFailure()
    {
        var dispatcher = CreateDispatcher(s => s.AddScoped<IRequestHandler<TestRequest, string>>(_ => new FailingHandler()));
        var request = new TestRequest("test");

        var result = await dispatcher.Dispatch(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Handler returned failure", result.Error);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task Dispatch_RethrowsOperationCanceledException()
    {
        var dispatcher = CreateDispatcher(s => s.AddScoped<IRequestHandler<ThrowingRequest, string>>(_ =>
            new CancellingHandler()));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            dispatcher.Dispatch(new ThrowingRequest("cancel")));
    }

    [Fact]
    public async Task Dispatch_PassesCancellationToken_ToHandler()
    {
        var receivedToken = CancellationToken.None;
        var handler = new TokenCapturingHandler(ct => receivedToken = ct);
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<TestRequest, string>>(handler));

        using var cts = new CancellationTokenSource();
        var request = new TestRequest("test");

        await dispatcher.Dispatch(request, cts.Token);

        Assert.Equal(cts.Token, receivedToken);
    }

    #region Test-specific Handlers

    private class CancellingHandler : IRequestHandler<ThrowingRequest, string>
    {
        public ValueTask<Result<string>> HandleAsync(ThrowingRequest request, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }

    private class TokenCapturingHandler(Action<CancellationToken> capture) : IRequestHandler<TestRequest, string>
    {
        public ValueTask<Result<string>> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            capture(cancellationToken);
            return new(Result<string>.Success("done"));
        }
    }

    private class FailingHandler : IRequestHandler<TestRequest, string>
    {
        public ValueTask<Result<string>> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return new(Result<string>.Failure("Handler returned failure"));
        }
    }

    #endregion
}
