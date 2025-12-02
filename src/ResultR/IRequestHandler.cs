namespace ResultR;

/// <summary>
/// Defines a handler for a request that does not return a value.
/// Handlers can optionally override lifecycle hooks for validation and before/after processing.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <remarks>
/// <para>
/// The dispatcher invokes the following pipeline in order:
/// </para>
/// <list type="bullet">
///   <item>
///     <term><see cref="ValidateAsync"/></term>
///     <description>Called before handling. If it returns a failure, the pipeline short-circuits.</description>
///   </item>
///   <item>
///     <term><see cref="BeforeHandleAsync"/></term>
///     <description>Called after validation passes, before the main handler executes.</description>
///   </item>
///   <item>
///     <term><see cref="HandleAsync"/></term>
///     <description>The main handler logic.</description>
///   </item>
///   <item>
///     <term><see cref="AfterHandleAsync"/></term>
///     <description>Called after the main handler executes, regardless of success or failure.</description>
///   </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public record DeleteUserRequest(int Id) : IRequest;
/// 
/// public class DeleteUserHandler : IRequestHandler&lt;DeleteUserRequest&gt;
/// {
///     public async ValueTask&lt;Result&gt; HandleAsync(DeleteUserRequest request, CancellationToken cancellationToken)
///     {
///         // Delete user logic
///         return Result.Success();
///     }
/// }
/// </code>
/// </example>
public interface IRequestHandler<TRequest>
    where TRequest : IRequest
{
    /// <summary>
    /// Validates the request before handling. Override to add validation logic.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A success result to continue, or a failure result to short-circuit the pipeline.</returns>
    virtual ValueTask<Result> ValidateAsync(TRequest request) => new(Result.Success());

    /// <summary>
    /// Called after validation passes, before the main handler executes. Override to add pre-processing logic.
    /// </summary>
    /// <param name="request">The request being handled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    virtual ValueTask BeforeHandleAsync(TRequest request) => default;

    /// <summary>
    /// Handles the request and returns a result indicating success or failure.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    ValueTask<Result> HandleAsync(TRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Called after the main handler executes. Override to add post-processing logic.
    /// </summary>
    /// <param name="request">The request that was handled.</param>
    /// <param name="result">The result from the handler.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    virtual ValueTask AfterHandleAsync(TRequest request, Result result) => default;
}

/// <summary>
/// Defines a handler for a request that returns a response wrapped in a <see cref="Result{TResponse}"/>.
/// Handlers can optionally override lifecycle hooks for validation and before/after processing.
/// </summary>
/// <typeparam name="TRequest">The type of request being handled.</typeparam>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
/// <remarks>
/// <para>
/// The dispatcher invokes the following pipeline in order:
/// </para>
/// <list type="bullet">
///   <item>
///     <term><see cref="ValidateAsync"/></term>
///     <description>Called before handling. If it returns a failure, the pipeline short-circuits.</description>
///   </item>
///   <item>
///     <term><see cref="BeforeHandleAsync"/></term>
///     <description>Called after validation passes, before the main handler executes.</description>
///   </item>
///   <item>
///     <term><see cref="HandleAsync"/></term>
///     <description>The main handler logic.</description>
///   </item>
///   <item>
///     <term><see cref="AfterHandleAsync"/></term>
///     <description>Called after the main handler executes, regardless of success or failure.</description>
///   </item>
/// </list>
/// </remarks>
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Validates the request before handling. Override to add validation logic.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <returns>A success result to continue, or a failure result to short-circuit the pipeline.</returns>
    virtual ValueTask<Result> ValidateAsync(TRequest request) => new(Result.Success());

    /// <summary>
    /// Called after validation passes, before the main handler executes. Override to add pre-processing logic.
    /// </summary>
    /// <param name="request">The request being handled.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    virtual ValueTask BeforeHandleAsync(TRequest request) => default;

    /// <summary>
    /// Handles the request and returns a result containing the response.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    ValueTask<Result<TResponse>> HandleAsync(TRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Called after the main handler executes. Override to add post-processing logic.
    /// </summary>
    /// <param name="request">The request that was handled.</param>
    /// <param name="result">The result from the handler.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    virtual ValueTask AfterHandleAsync(TRequest request, Result<TResponse> result) => default;
}
