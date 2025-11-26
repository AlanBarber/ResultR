using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ResultR;

/// <summary>
/// Default implementation of <see cref="IMediator"/> that dispatches requests through a pipeline
/// consisting of validation, pre-handle, handle, and post-handle phases.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    // Cache for compiled handler invokers keyed by (handlerType, responseType)
    private static readonly ConcurrentDictionary<(Type HandlerType, Type ResponseType), object> _invokerCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers.</param>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        var concreteHandlerType = handler.GetType();
        var invoker = (HandlerInvoker<TResponse>)_invokerCache.GetOrAdd(
            (concreteHandlerType, typeof(TResponse)),
            key => CreateInvoker<TResponse>(key.HandlerType, requestType));

        return await invoker.InvokeAsync(handler, request, cancellationToken);
    }

    /// <summary>
    /// Creates a compiled invoker for the given handler type, caching all method lookups and delegate compilations.
    /// </summary>
    private static HandlerInvoker<TResponse> CreateInvoker<TResponse>(Type handlerType, Type requestType)
    {
        // Build compiled delegates for each pipeline step
        var validateDelegate = CreateValidateDelegate(handlerType, requestType);
        var preHandleDelegate = CreatePreHandleDelegate(handlerType, requestType);
        var handleDelegate = CreateHandleDelegate<TResponse>(handlerType, requestType);
        var postHandleDelegate = CreatePostHandleDelegate<TResponse>(handlerType, requestType);

        return new HandlerInvoker<TResponse>(validateDelegate, preHandleDelegate, handleDelegate, postHandleDelegate);
    }

    private static Func<object, object, Result?>? CreateValidateDelegate(Type handlerType, Type requestType)
    {
        var method = handlerType.GetMethod("Validate", [requestType]);
        if (method is null) return null;

        // Parameters: (object handler, object request) => Result?
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var requestParam = Expression.Parameter(typeof(object), "request");

        var call = Expression.Call(
            Expression.Convert(handlerParam, handlerType),
            method,
            Expression.Convert(requestParam, requestType));

        var lambda = Expression.Lambda<Func<object, object, Result?>>(
            Expression.Convert(call, typeof(Result)),
            handlerParam, requestParam);

        return lambda.Compile();
    }

    private static Action<object, object>? CreatePreHandleDelegate(Type handlerType, Type requestType)
    {
        var method = handlerType.GetMethod("OnPreHandle", [requestType]);
        if (method is null) return null;

        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var requestParam = Expression.Parameter(typeof(object), "request");

        var call = Expression.Call(
            Expression.Convert(handlerParam, handlerType),
            method,
            Expression.Convert(requestParam, requestType));

        var lambda = Expression.Lambda<Action<object, object>>(call, handlerParam, requestParam);
        return lambda.Compile();
    }

    private static Func<object, object, CancellationToken, Task<Result<TResponse>>> CreateHandleDelegate<TResponse>(Type handlerType, Type requestType)
    {
        var method = handlerType.GetMethod("Handle", [requestType, typeof(CancellationToken)])
            ?? throw new InvalidOperationException($"Handler {handlerType.Name} does not have a Handle method");

        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var requestParam = Expression.Parameter(typeof(object), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var call = Expression.Call(
            Expression.Convert(handlerParam, handlerType),
            method,
            Expression.Convert(requestParam, requestType),
            ctParam);

        var lambda = Expression.Lambda<Func<object, object, CancellationToken, Task<Result<TResponse>>>>(
            call, handlerParam, requestParam, ctParam);

        return lambda.Compile();
    }

    private static Action<object, object, Result<TResponse>>? CreatePostHandleDelegate<TResponse>(Type handlerType, Type requestType)
    {
        var method = handlerType.GetMethod("OnPostHandle", [requestType, typeof(Result<TResponse>)]);
        if (method is null) return null;

        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var requestParam = Expression.Parameter(typeof(object), "request");
        var resultParam = Expression.Parameter(typeof(Result<TResponse>), "result");

        var call = Expression.Call(
            Expression.Convert(handlerParam, handlerType),
            method,
            Expression.Convert(requestParam, requestType),
            resultParam);

        var lambda = Expression.Lambda<Action<object, object, Result<TResponse>>>(call, handlerParam, requestParam, resultParam);
        return lambda.Compile();
    }

    /// <summary>
    /// Holds compiled delegates for a specific handler type, enabling fast pipeline execution.
    /// </summary>
    private sealed class HandlerInvoker<TResponse>
    {
        private readonly Func<object, object, Result?>? _validate;
        private readonly Action<object, object>? _preHandle;
        private readonly Func<object, object, CancellationToken, Task<Result<TResponse>>> _handle;
        private readonly Action<object, object, Result<TResponse>>? _postHandle;

        public HandlerInvoker(
            Func<object, object, Result?>? validate,
            Action<object, object>? preHandle,
            Func<object, object, CancellationToken, Task<Result<TResponse>>> handle,
            Action<object, object, Result<TResponse>>? postHandle)
        {
            _validate = validate;
            _preHandle = preHandle;
            _handle = handle;
            _postHandle = postHandle;
        }

        public async Task<Result<TResponse>> InvokeAsync(object handler, object request, CancellationToken cancellationToken)
        {
            // Step 1: Validate (if present)
            if (_validate is not null)
            {
                var validationResult = _validate(handler, request);
                if (validationResult?.IsFailure == true)
                {
                    return Result<TResponse>.Failure(
                        validationResult.Error ?? "Validation failed",
                        validationResult.Exception!);
                }
            }

            // Step 2: OnPreHandle (if present)
            _preHandle?.Invoke(handler, request);

            // Step 3: Handle (required)
            var result = await _handle(handler, request, cancellationToken);

            // Step 4: OnPostHandle (if present)
            _postHandle?.Invoke(handler, request, result);

            return result;
        }
    }
}
