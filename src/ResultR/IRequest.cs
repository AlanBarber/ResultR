namespace ResultR;

/// <summary>
/// Marker interface for a request that returns a response of type <typeparamref name="TResponse"/>.
/// All requests in ResultR implement this interface regardless of whether they are commands or queries.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
public interface IRequest<TResponse>;
