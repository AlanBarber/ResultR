using Microsoft.Extensions.DependencyInjection;

namespace ResultR.Tests.DispatcherTests;

/// <summary>
/// Tests for Dispatcher functionality with void request handlers (IRequest without response).
/// </summary>
public class DispatcherVoidRequestTests : DispatcherTestBase
{
    [Fact]
    public async Task Dispatch_VoidRequest_WithValidHandler_ReturnsSuccessResult()
    {
        var handler = new VoidHandler();
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<VoidRequest>>(handler));
        var request = new VoidRequest(42);

        var result = await dispatcher.Dispatch(request);

        Assert.True(result.IsSuccess);
        Assert.True(handler.HandleCalled);
        Assert.Equal(42, handler.ReceivedId);
    }

    [Fact]
    public async Task Dispatch_VoidRequest_WithNoHandler_ThrowsException()
    {
        var dispatcher = CreateDispatcher(_ => { });
        var request = new VoidRequest(1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.Dispatch(request));
    }

    [Fact]
    public async Task Dispatch_VoidRequest_WithNullRequest_ThrowsArgumentNullException()
    {
        var handler = new VoidHandler();
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<VoidRequest>>(handler));

        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.Dispatch((IRequest)null!));
    }

    [Fact]
    public async Task Dispatch_VoidRequest_ExecutesFullPipeline()
    {
        var handler = new ValidatedVoidHandler();
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<ValidatedVoidRequest>>(handler));
        var request = new ValidatedVoidRequest("Test");

        var result = await dispatcher.Dispatch(request);

        Assert.True(result.IsSuccess);
        Assert.True(handler.ValidateCalled);
        Assert.True(handler.PreHandleCalled);
        Assert.True(handler.HandleCalled);
        Assert.True(handler.PostHandleCalled);
    }

    [Fact]
    public async Task Dispatch_VoidRequest_ShortCircuits_WhenValidationFails()
    {
        var handler = new ValidatedVoidHandler();
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<ValidatedVoidRequest>>(handler));
        var request = new ValidatedVoidRequest(""); // Empty name should fail validation

        var result = await dispatcher.Dispatch(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Name is required", result.Error);
        Assert.True(handler.ValidateCalled);
        Assert.False(handler.HandleCalled); // Handle should not be called when validation fails
    }

    [Fact]
    public async Task Dispatch_VoidRequest_ReturnsFailure_WhenHandlerReturnsFailure()
    {
        var dispatcher = CreateDispatcher(s => s.AddScoped<IRequestHandler<VoidRequest>>(_ => new FailingVoidHandler()));
        var request = new VoidRequest(1);

        var result = await dispatcher.Dispatch(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Operation failed", result.Error);
    }

    [Fact]
    public async Task Dispatch_VoidRequest_ReturnsFailure_WhenHandlerThrowsException()
    {
        var dispatcher = CreateDispatcher(s => s.AddScoped<IRequestHandler<VoidRequest>>(_ => new ThrowingVoidHandler()));
        var request = new VoidRequest(1);

        var result = await dispatcher.Dispatch(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong", result.Error);
        Assert.NotNull(result.Exception);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    [Fact]
    public async Task Dispatch_VoidRequest_RethrowsOperationCanceledException()
    {
        var dispatcher = CreateDispatcher(s => s.AddScoped<IRequestHandler<VoidRequest>>(_ => new CancellingVoidHandler()));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            dispatcher.Dispatch(new VoidRequest(1)));
    }

    [Fact]
    public async Task Dispatch_VoidRequest_PassesCancellationToken_ToHandler()
    {
        var receivedToken = CancellationToken.None;
        var handler = new TokenCapturingVoidHandler(ct => receivedToken = ct);
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<VoidRequest>>(handler));

        using var cts = new CancellationTokenSource();
        var request = new VoidRequest(1);

        await dispatcher.Dispatch(request, cts.Token);

        Assert.Equal(cts.Token, receivedToken);
    }

    #region Test Fixtures

    public record ValidatedVoidRequest(string Name) : IRequest;

    public class ValidatedVoidHandler : IRequestHandler<ValidatedVoidRequest>
    {
        public bool ValidateCalled { get; private set; }
        public bool PreHandleCalled { get; private set; }
        public bool HandleCalled { get; private set; }
        public bool PostHandleCalled { get; private set; }

        public ValueTask<Result> ValidateAsync(ValidatedVoidRequest request)
        {
            ValidateCalled = true;
            return new(string.IsNullOrEmpty(request.Name)
                ? Result.Failure("Name is required")
                : Result.Success());
        }

        public ValueTask BeforeHandleAsync(ValidatedVoidRequest request)
        {
            PreHandleCalled = true;
            return default;
        }

        public ValueTask<Result> HandleAsync(ValidatedVoidRequest request, CancellationToken cancellationToken)
        {
            HandleCalled = true;
            return new(Result.Success());
        }

        public ValueTask AfterHandleAsync(ValidatedVoidRequest request, Result result)
        {
            PostHandleCalled = true;
            return default;
        }
    }

    private class FailingVoidHandler : IRequestHandler<VoidRequest>
    {
        public ValueTask<Result> HandleAsync(VoidRequest request, CancellationToken cancellationToken)
        {
            return new(Result.Failure("Operation failed"));
        }
    }

    private class ThrowingVoidHandler : IRequestHandler<VoidRequest>
    {
        public ValueTask<Result> HandleAsync(VoidRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Something went wrong");
        }
    }

    private class CancellingVoidHandler : IRequestHandler<VoidRequest>
    {
        public ValueTask<Result> HandleAsync(VoidRequest request, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }

    private class TokenCapturingVoidHandler(Action<CancellationToken> capture) : IRequestHandler<VoidRequest>
    {
        public ValueTask<Result> HandleAsync(VoidRequest request, CancellationToken cancellationToken)
        {
            capture(cancellationToken);
            return new(Result.Success());
        }
    }

    #endregion
}
