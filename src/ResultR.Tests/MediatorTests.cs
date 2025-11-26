using Microsoft.Extensions.DependencyInjection;

namespace ResultR.Tests;

public class MediatorTests
{
    #region Test Fixtures

    public record TestRequest(string Value) : IRequest<string>;

    public class TestHandler : IRequestHandler<TestRequest, string>
    {
        public Task<Result<string>> Handle(TestRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<string>.Success($"Handled: {request.Value}"));
        }
    }

    public record ValidatedRequest(string Email) : IRequest<bool>;

    public class ValidatedHandler : IRequestHandler<ValidatedRequest, bool>
    {
        public bool ValidateCalled { get; private set; }
        public bool PreHandleCalled { get; private set; }
        public bool PostHandleCalled { get; private set; }
        public Result<bool>? PostHandleResult { get; private set; }

        public Result Validate(ValidatedRequest request)
        {
            ValidateCalled = true;
            return string.IsNullOrEmpty(request.Email)
                ? Result.Failure("Email is required")
                : Result.Success();
        }

        public void OnPreHandle(ValidatedRequest request)
        {
            PreHandleCalled = true;
        }

        public Task<Result<bool>> Handle(ValidatedRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<bool>.Success(true));
        }

        public void OnPostHandle(ValidatedRequest request, Result<bool> result)
        {
            PostHandleCalled = true;
            PostHandleResult = result;
        }
    }

    public record FailingValidationRequest(int Value) : IRequest<int>;

    public class FailingValidationHandler : IRequestHandler<FailingValidationRequest, int>
    {
        public bool HandleCalled { get; private set; }

        public Result Validate(FailingValidationRequest request)
        {
            return request.Value < 0
                ? Result.Failure("Value must be non-negative")
                : Result.Success();
        }

        public Task<Result<int>> Handle(FailingValidationRequest request, CancellationToken cancellationToken)
        {
            HandleCalled = true;
            return Task.FromResult(Result<int>.Success(request.Value));
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
}
