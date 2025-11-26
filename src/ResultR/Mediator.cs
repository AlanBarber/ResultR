using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace ResultR;

/// <summary>
/// Default implementation of <see cref="IMediator"/> that dispatches requests through a pipeline
/// consisting of validation, pre-handle, handle, and post-handle phases.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    // Cache for constructed handler types: (requestType, responseType) -> handlerInterfaceType
    private static readonly ConcurrentDictionary<(Type, Type), Type> _handlerTypeCache = new();

    // Cache for compiled pipeline executors: handlerType -> executor delegate
    private static readonly ConcurrentDictionary<Type, Delegate> _pipelineExecutorCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers.</param>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
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
    /// </summary>
    private static Delegate CreatePipelineExecutor(Type handlerType)
    {
        // Extract TRequest and TResponse from IRequestHandler<TRequest, TResponse>
        var genericArgs = handlerType.GetGenericArguments();
        var requestType = genericArgs[0];
        var responseType = genericArgs[1];

        // Create the generic method ExecutePipelineAsync<TRequest, TResponse>
        var method = typeof(Mediator)
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
    /// Executes the handler pipeline with direct interface method calls.
    /// Exceptions are caught and returned as failure results.
    /// </summary>
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

            // Step 2: OnPreHandle
            await handler.OnPreHandleAsync(request);

            // Step 3: Handle
            var result = await handler.HandleAsync(request, cancellationToken);

            // Step 4: OnPostHandle
            await handler.OnPostHandleAsync(request, result);

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
