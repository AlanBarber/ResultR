namespace ResultR;

/// <summary>
/// Defines the dispatcher that routes requests to their corresponding handlers.
/// </summary>
/// <remarks>
/// <para>
/// This interface follows a request/response dispatcher pattern (sometimes called an in-process message bus),
/// which routes requests to exactly one handler. This differs from the classic GoF Mediator pattern,
/// which coordinates bidirectional communication between multiple colleague objects.
/// </para>
/// <para>
/// The name "Dispatcher" more accurately reflects the routing behavior: requests go in,
/// responses come out, with no inter-handler communication.
/// </para>
/// </remarks>
public interface IDispatcher
{
    /// <summary>
    /// Dispatches a request that does not return a value to its handler.
    /// </summary>
    /// <param name="request">The request to dispatch.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    Task<Result> Dispatch(IRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches a request to its handler and returns the result.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the handler.</typeparam>
    /// <param name="request">The request to dispatch.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    Task<Result<TResponse>> Dispatch<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
