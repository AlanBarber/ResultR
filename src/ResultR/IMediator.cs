namespace ResultR;

/// <summary>
/// Defines the mediator that dispatches requests to their corresponding handlers.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a request to its handler and returns the result.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the handler.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    Task<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
