namespace ResultR;

/// <summary>
/// Defines a handler for a request that returns a response wrapped in a <see cref="Result{TResponse}"/>.
/// Handlers can optionally implement validation and lifecycle hooks by defining the appropriate methods.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
/// <remarks>
/// <para>
/// The mediator will automatically detect and invoke the following optional methods if present:
/// </para>
/// <list type="bullet">
///   <item>
///     <term><c>Result Validate(TRequest request)</c></term>
///     <description>Called before handling. If it returns a failure, the pipeline short-circuits.</description>
///   </item>
///   <item>
///     <term><c>void OnPreHandle(TRequest request)</c></term>
///     <description>Called after validation passes, before the main handler executes.</description>
///   </item>
///   <item>
///     <term><c>void OnPostHandle(TRequest request, Result&lt;TResponse&gt; result)</c></term>
///     <description>Called after the main handler executes, regardless of success or failure.</description>
///   </item>
/// </list>
/// </remarks>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the request and returns a result containing the response.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    Task<Result<TResponse>> Handle(TRequest request, CancellationToken cancellationToken);
}
