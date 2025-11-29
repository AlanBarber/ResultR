using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace ResultR;

/// <summary>
/// Default implementation of <see cref="IDispatcher"/> that routes requests through a pipeline
/// consisting of validation, before-handle, handle, and after-handle phases.
/// </summary>
/// <remarks>
/// <para>
/// This class implements a request/response dispatcher pattern, routing each request to exactly
/// one handler. Despite common naming conventions in the .NET ecosystem (e.g., MediatR), this is
/// technically closer to a Command pattern or in-process message bus than the classic GoF Mediator pattern.
/// </para>
/// </remarks>
public sealed class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    // Single cache: requestType -> (handlerType, compiled executor)
    // Using requestType as key avoids tuple allocation and reduces lookups from 2 to 1
    private static readonly ConcurrentDictionary<Type, (Type HandlerType, Delegate Executor)> _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Dispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers.</param>
    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<Result<TResponse>> Dispatch<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        // Single cache lookup for both handler type and executor
        var (handlerType, executor) = _cache.GetOrAdd(
            requestType,
            static type => CreateCacheEntry(type));

        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        // Execute the cached delegate - cast is safe because we control delegate creation
        return Unsafe.As<Func<object, object, CancellationToken, Task<Result<TResponse>>>>(executor)(
            handler, request, cancellationToken);
    }

    /// <summary>
    /// Creates a cache entry containing both the handler type and compiled executor.
    /// Called once per request type, then cached for all subsequent dispatches.
    /// </summary>
    private static (Type HandlerType, Delegate Executor) CreateCacheEntry(Type requestType)
    {
        // Find TResponse from IRequest<TResponse> interface
        var requestInterface = requestType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
            ?? throw new InvalidOperationException(
                $"Type '{requestType.Name}' does not implement IRequest<TResponse>. " +
                "Ensure your request type implements IRequest<TResponse>.");
        var responseType = requestInterface.GetGenericArguments()[0];

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var executor = CreatePipelineExecutor(handlerType, requestType, responseType);

        return (handlerType, executor);
    }

    /// <summary>
    /// Creates a compiled delegate that executes the pipeline for a specific handler type.
    /// Uses expression trees to build a strongly-typed delegate at runtime, avoiding reflection
    /// on every request. The compiled delegate is cached for subsequent calls.
    /// </summary>
    private static Delegate CreatePipelineExecutor(Type handlerType, Type requestType, Type responseType)
    {
        // Create the generic method ExecutePipelineAsync<TRequest, TResponse>
        var method = typeof(Dispatcher)
            .GetMethod(nameof(ExecutePipelineAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(requestType, responseType);

        // Build expression: (object handler, object request, CancellationToken ct) => 
        //     ExecutePipelineAsync<TRequest, TResponse>((IRequestHandler<TRequest, TResponse>)handler, (TRequest)request, ct)
        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var requestParam = Expression.Parameter(typeof(object), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "cancellationToken");

        var call = Expression.Call(
            method,
            Expression.Convert(handlerParam, handlerType),
            Expression.Convert(requestParam, requestType),
            ctParam);

        var lambda = Expression.Lambda(call, handlerParam, requestParam, ctParam);
        return lambda.Compile();
    }

    /// <summary>
    /// Executes the full handler pipeline: Validate → BeforeHandle → Handle → AfterHandle.
    /// </summary>
    /// <remarks>
    /// <para>The pipeline executes in order:</para>
    /// <list type="number">
    ///   <item>ValidateAsync - if validation fails, returns early with failure result</item>
    ///   <item>BeforeHandleAsync - runs pre-processing logic (e.g., logging)</item>
    ///   <item>HandleAsync - executes the main business logic</item>
    ///   <item>AfterHandleAsync - runs post-processing logic (e.g., logging, cleanup)</item>
    /// </list>
    /// <para>
    /// Any exception (except OperationCanceledException) is caught and
    /// converted to a failure result with the exception attached.
    /// </para>
    /// </remarks>
    private static async Task<Result<TResponse>> ExecutePipelineAsync<TRequest, TResponse>(
        IRequestHandler<TRequest, TResponse> handler,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        try
        {
            // Step 1: Validate
            var validationResult = await handler.ValidateAsync(request).ConfigureAwait(false);
            if (validationResult.IsFailure)
            {
                // Preserve exception from validation if present, otherwise just use error message
                return validationResult.Exception is not null
                    ? Result<TResponse>.Failure(validationResult.Error ?? "Validation failed", validationResult.Exception)
                    : Result<TResponse>.Failure(validationResult.Error ?? "Validation failed");
            }

            // Step 2: BeforeHandle
            await handler.BeforeHandleAsync(request).ConfigureAwait(false);

            // Step 3: Handle
            var result = await handler.HandleAsync(request, cancellationToken).ConfigureAwait(false);

            // Step 4: AfterHandle
            await handler.AfterHandleAsync(request, result).ConfigureAwait(false);

            return result;
        }
        catch (OperationCanceledException)
        {
            // Re-throw cancellation to allow proper async cancellation handling
            throw;
        }
        catch (Exception ex)
        {
            return Result<TResponse>.Failure(ex.Message, ex);
        }
    }
}
