namespace ResultR;

/// <summary>
/// Marker interface for a request that does not return a value.
/// Use this for commands or operations where you only need success/failure indication.
/// </summary>
/// <example>
/// <code>
/// public record DeleteUserRequest(int Id) : IRequest;
/// </code>
/// </example>
public interface IRequest;

/// <summary>
/// Marker interface for a request that returns a response of type <typeparamref name="TResponse"/>.
/// All requests in ResultR implement this interface regardless of whether they are commands or queries.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
public interface IRequest<TResponse>;
