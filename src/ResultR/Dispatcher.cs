using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

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

    // Cache for constructed handler types: (requestType, responseType) -> handlerInterfaceType
    private static readonly ConcurrentDictionary<(Type, Type), Type> _handlerTypeCache = new();

    // Cache for compiled pipeline executors: handlerType -> executor delegate
    private static readonly ConcurrentDictionary<Type, Delegate> _pipelineExecutorCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Dispatcher"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers.</param>
    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<Result<TResponse>> Dispatch<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        // Get or create the handler interface type from cache
        var handlerType = _handlerTypeCache.GetOrAdd(
            (requestType, typeof(TResponse)),
            static key => typeof(IRequestHandler<,>).MakeGenericType(key.Item1, key.Item2));

        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        // Get or create the compiled pipeline executor from cache
        var executor = _pipelineExecutorCache.GetOrAdd(
            handlerType,
            static type => CreatePipelineExecutor(type));

        // Execute the cached delegate
        return Unsafe.As<Func<object, object, CancellationToken, Task<Result<TResponse>>>>(executor)(
            handler, request, cancellationToken);
    }

    /// <summary>
    /// Creates a compiled delegate that executes the pipeline for a specific handler type.
    /// Uses expression trees to build a strongly-typed delegate at runtime, avoiding reflection
    /// on every request. The compiled delegate is cached for subsequent calls.
    /// </summary>
    /// <param name="handlerType">The closed generic handler interface type (e.g., IRequestHandler{MyRequest, MyResponse}).</param>
    /// <returns>A delegate that can execute the pipeline for the given handler type.</returns>
    private static Delegate CreatePipelineExecutor(Type handlerType)
    {
        // Extract TRequest and TResponse from IRequestHandler<TRequest, TResponse>
        var genericArgs = handlerType.GetGenericArguments();
        var requestType = genericArgs[0];
        var responseType = genericArgs[1];

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
            var validationResult = await handler.ValidateAsync(request);
            if (validationResult.IsFailure)
            {
                return Result<TResponse>.Failure(validationResult.Error ?? "Validation failed");
            }

            // Step 2: BeforeHandle
            await handler.BeforeHandleAsync(request);

            // Step 3: Handle
            var result = await handler.HandleAsync(request, cancellationToken);

            // Step 4: AfterHandle
            await handler.AfterHandleAsync(request, result);

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
