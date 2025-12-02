using Microsoft.Extensions.DependencyInjection;

namespace ResultR.Tests.DispatcherTests;

/// <summary>
/// Tests for Dispatcher pipeline behavior (validation, before/after handle hooks).
/// </summary>
public class DispatcherPipelineTests : DispatcherTestBase
{
    [Fact]
    public async Task Dispatch_CallsValidateMethod_WhenPresent()
    {
        var handler = new ValidatedHandler();
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<ValidatedRequest, bool>>(handler));
        var request = new ValidatedRequest("test@example.com");

        await dispatcher.Dispatch(request);

        Assert.True(handler.ValidateCalled);
    }

    [Fact]
    public async Task Dispatch_CallsBeforeHandleMethod_WhenPresent()
    {
        var handler = new ValidatedHandler();
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<ValidatedRequest, bool>>(handler));
        var request = new ValidatedRequest("test@example.com");

        await dispatcher.Dispatch(request);

        Assert.True(handler.PreHandleCalled);
    }

    [Fact]
    public async Task Dispatch_CallsAfterHandleMethod_WhenPresent()
    {
        var handler = new ValidatedHandler();
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<ValidatedRequest, bool>>(handler));
        var request = new ValidatedRequest("test@example.com");

        await dispatcher.Dispatch(request);

        Assert.True(handler.PostHandleCalled);
        Assert.NotNull(handler.PostHandleResult);
        Assert.True(handler.PostHandleResult.IsSuccess);
    }

    [Fact]
    public async Task Dispatch_ShortCircuits_WhenValidationFails()
    {
        var handler = new FailingValidationHandler();
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<FailingValidationRequest, int>>(handler));
        var request = new FailingValidationRequest(-1);

        var result = await dispatcher.Dispatch(request);

        Assert.True(result.IsFailure);
        Assert.Equal("Value must be non-negative", result.Error);
        Assert.False(handler.HandleCalled); // Handle should not be called when validation fails
    }

    [Fact]
    public async Task Dispatch_ExecutesHandle_WhenValidationPasses()
    {
        var handler = new FailingValidationHandler();
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<FailingValidationRequest, int>>(handler));
        var request = new FailingValidationRequest(42);

        var result = await dispatcher.Dispatch(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.True(handler.HandleCalled);
    }

    [Fact]
    public async Task Dispatch_PreservesValidationException_WhenPresent()
    {
        var expectedException = new ArgumentException("Invalid data");
        var handler = new ValidationWithExceptionHandler(expectedException);
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<TestRequest, string>>(handler));

        var result = await dispatcher.Dispatch(new TestRequest("test"));

        Assert.True(result.IsFailure);
        Assert.Same(expectedException, result.Exception);
    }

    [Fact]
    public async Task Dispatch_SkipsBeforeAndAfterHandle_WhenValidationFails()
    {
        var handler = new FullPipelineTrackingHandler(shouldFailValidation: true);
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<PipelineTrackingRequest, string>>(handler));

        var result = await dispatcher.Dispatch(new PipelineTrackingRequest("test"));

        Assert.True(result.IsFailure);
        Assert.True(handler.ValidateCalled);
        Assert.False(handler.BeforeHandleCalled); // Should be skipped when validation fails
        Assert.False(handler.HandleCalled);
        Assert.False(handler.AfterHandleCalled); // Should be skipped when validation fails
    }

    [Fact]
    public async Task Dispatch_AfterHandleReceivesFailureResult_WhenHandlerFails()
    {
        var handler = new FullPipelineTrackingHandler(shouldFailValidation: false, shouldFailHandle: true);
        var dispatcher = CreateDispatcher(s => s.AddSingleton<IRequestHandler<PipelineTrackingRequest, string>>(handler));

        var result = await dispatcher.Dispatch(new PipelineTrackingRequest("test"));

        Assert.True(result.IsFailure);
        Assert.True(handler.AfterHandleCalled);
        Assert.NotNull(handler.AfterHandleResult);
        Assert.True(handler.AfterHandleResult.IsFailure);
        Assert.Equal("Handler failed", handler.AfterHandleResult.Error);
    }

    #region Test Fixtures

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

        public ValueTask BeforeHandleAsync(ValidatedRequest request)
        {
            PreHandleCalled = true;
            return default;
        }

        public ValueTask<Result<bool>> HandleAsync(ValidatedRequest request, CancellationToken cancellationToken)
        {
            return new(Result<bool>.Success(true));
        }

        public ValueTask AfterHandleAsync(ValidatedRequest request, Result<bool> result)
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

    private class ValidationWithExceptionHandler(Exception exception) : IRequestHandler<TestRequest, string>
    {
        public ValueTask<Result> ValidateAsync(TestRequest request)
        {
            return new(Result.Failure("Validation failed", exception));
        }

        public ValueTask<Result<string>> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return new(Result<string>.Success("should not reach"));
        }
    }

    public record PipelineTrackingRequest(string Value) : IRequest<string>;

    public class FullPipelineTrackingHandler(bool shouldFailValidation = false, bool shouldFailHandle = false)
        : IRequestHandler<PipelineTrackingRequest, string>
    {
        public bool ValidateCalled { get; private set; }
        public bool BeforeHandleCalled { get; private set; }
        public bool HandleCalled { get; private set; }
        public bool AfterHandleCalled { get; private set; }
        public Result<string>? AfterHandleResult { get; private set; }

        public ValueTask<Result> ValidateAsync(PipelineTrackingRequest request)
        {
            ValidateCalled = true;
            return new(shouldFailValidation
                ? Result.Failure("Validation failed")
                : Result.Success());
        }

        public ValueTask BeforeHandleAsync(PipelineTrackingRequest request)
        {
            BeforeHandleCalled = true;
            return default;
        }

        public ValueTask<Result<string>> HandleAsync(PipelineTrackingRequest request, CancellationToken cancellationToken)
        {
            HandleCalled = true;
            return new(shouldFailHandle
                ? Result<string>.Failure("Handler failed")
                : Result<string>.Success("Success"));
        }

        public ValueTask AfterHandleAsync(PipelineTrackingRequest request, Result<string> result)
        {
            AfterHandleCalled = true;
            AfterHandleResult = result;
            return default;
        }
    }

    #endregion
}
