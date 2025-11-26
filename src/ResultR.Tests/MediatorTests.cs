using Microsoft.Extensions.DependencyInjection;

namespace ResultR.Tests;

public class MediatorTests
{
    #region Test Fixtures

    public record TestRequest(string Value) : IRequest<string>;

    public class TestHandler : IRequestHandler<TestRequest, string>
    {
        public ValueTask<Result<string>> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return new(Result<string>.Success($"Handled: {request.Value}"));
        }
    }

    public record ValidatedRequest(string Email) : IRequest<bool>;

    public class ValidatedHandler : IRequestHandler<ValidatedRequest, bool>
    {
        public bool ValidateCalled { get; private set; }
        public bool PreHandleCalled { get; private set; }
        public bool PostHandleCalled { get; private set; }
        public Result<bool>? PostHandleResult { get; private set; }

        public ValueTask<Result> ValidateAsync(ValidatedRequest request)
        {
            ValidateCalled = true;
            return new(string.IsNullOrEmpty(request.Email)
                ? Result.Failure("Email is required")
                : Result.Success());
        }

        public ValueTask OnPreHandleAsync(ValidatedRequest request)
        {
            PreHandleCalled = true;
            return default;
        }

        public ValueTask<Result<bool>> HandleAsync(ValidatedRequest request, CancellationToken cancellationToken)
        {
            return new(Result<bool>.Success(true));
        }

        public ValueTask OnPostHandleAsync(ValidatedRequest request, Result<bool> result)
        {
            PostHandleCalled = true;
            PostHandleResult = result;
            return default;
        }
    }

    public record FailingValidationRequest(int Value) : IRequest<int>;

    public class FailingValidationHandler : IRequestHandler<FailingValidationRequest, int>
    {
        public bool HandleCalled { get; private set; }

        public ValueTask<Result> ValidateAsync(FailingValidationRequest request)
        {
            return new(request.Value < 0
                ? Result.Failure("Value must be non-negative")
                : Result.Success());
        }

        public ValueTask<Result<int>> HandleAsync(FailingValidationRequest request, CancellationToken cancellationToken)
        {
            HandleCalled = true;
            return new(Result<int>.Success(request.Value));
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

    private static IMediator CreateMediator(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        services.AddScoped<IMediator, Mediator>();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IMediator>();
    }

    [Fact]
    public async Task Send_WithValidHandler_ReturnsSuccessResult()
    {
        var mediator = CreateMediator(s => s.AddScoped<IRequestHandler<TestRequest, string>, TestHandler>());
        var request = new TestRequest("Hello");

        var result = await mediator.Send(request);

        Assert.True(result.IsSuccess);
        Assert.Equal("Handled: Hello", result.Value);
    }

    [Fact]
    public async Task Send_WithNoHandler_ThrowsException()
    {
        var mediator = CreateMediator(_ => { });
        var request = new TestRequest("Hello");

        await Assert.ThrowsAsync<InvalidOperationException>(() => mediator.Send(request));
    }

    [Fact]
    public async Task Send_WithNullRequest_ThrowsArgumentNullException()
    {
        var mediator = CreateMediator(s => s.AddScoped<IRequestHandler<TestRequest, string>, TestHandler>());

        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Send<string>(null!));
    }

    [Fact]
    public async Task Send_CallsValidateMethod_WhenPresent()
    {
        var handler = new ValidatedHandler();
        var mediator = CreateMediator(s => s.AddSingleton<IRequestHandler<ValidatedRequest, bool>>(handler));
        var request = new ValidatedRequest("test@example.com");

        await mediator.Send(request);

        Assert.True(handler.ValidateCalled);
    }

    [Fact]
    public async Task Send_CallsPreHandleMethod_WhenPresent()
    {
        var handler = new ValidatedHandler();
        var mediator = CreateMediator(s => s.AddSingleton<IRequestHandler<ValidatedRequest, bool>>(handler));
        var request = new ValidatedRequest("test@example.com");

        await mediator.Send(request);

        Assert.True(handler.PreHandleCalled);
    }

    [Fact]
    public async Task Send_CallsPostHandleMethod_WhenPresent()
    {
        var handler = new ValidatedHandler();
        var mediator = CreateMediator(s => s.AddSingleton<IRequestHandler<ValidatedRequest, bool>>(handler));
        var request = new ValidatedRequest("test@example.com");

        await mediator.Send(request);

        Assert.True(handler.PostHandleCalled);
        Assert.NotNull(handler.PostHandleResult);
        Assert.True(handler.PostHandleResult.IsSuccess);
    }

    [Fact]
    public async Task Send_ShortCircuits_WhenValidationFails()
    {
        var handler = new FailingValidationHandler();
        var mediator = CreateMediator(s => s.AddSingleton<IRequestHandler<FailingValidationRequest, int>>(handler));
        var request = new FailingValidationRequest(-1);

        var result = await mediator.Send(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Value must be non-negative", result.Error);
        Assert.False(handler.HandleCalled); // Handle should not be called when validation fails
    }

    [Fact]
    public async Task Send_ExecutesHandle_WhenValidationPasses()
    {
        var handler = new FailingValidationHandler();
        var mediator = CreateMediator(s => s.AddSingleton<IRequestHandler<FailingValidationRequest, int>>(handler));
        var request = new FailingValidationRequest(42);

        var result = await mediator.Send(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.True(handler.HandleCalled);
    }

    [Fact]
    public async Task Send_ReturnsFailureResult_WhenHandlerThrowsException()
    {
        var mediator = CreateMediator(s => s.AddScoped<IRequestHandler<ThrowingRequest, string>, ThrowingHandler>());
        var request = new ThrowingRequest("Something went wrong");

        var result = await mediator.Send(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong", result.Error);
        Assert.NotNull(result.Exception);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    [Fact]
    public async Task Send_RethrowsOperationCanceledException()
    {
        var mediator = CreateMediator(s => s.AddScoped<IRequestHandler<ThrowingRequest, string>>(_ =>
            new CancellingHandler()));

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            mediator.Send(new ThrowingRequest("cancel")));
    }

    private class CancellingHandler : IRequestHandler<ThrowingRequest, string>
    {
        public ValueTask<Result<string>> HandleAsync(ThrowingRequest request, CancellationToken cancellationToken)
        {
            throw new OperationCanceledException();
        }
    }
}
