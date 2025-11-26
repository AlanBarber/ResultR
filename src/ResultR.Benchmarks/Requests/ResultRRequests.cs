namespace ResultR.Benchmarks.Requests;

// Simple request/response
public record ResultRSimpleRequest(int Value) : IRequest<int>;

public class ResultRSimpleHandler : IRequestHandler<ResultRSimpleRequest, int>
{
    public ValueTask<Result<int>> HandleAsync(ResultRSimpleRequest request, CancellationToken cancellationToken)
    {
        return new(Result<int>.Success(request.Value * 2));
    }
}

// Request with validation
public record ResultRValidatedRequest(int Value) : IRequest<int>;

public class ResultRValidatedHandler : IRequestHandler<ResultRValidatedRequest, int>
{
    public ValueTask<Result> ValidateAsync(ResultRValidatedRequest request)
    {
        return new(request.Value >= 0 ? Result.Success() : Result.Failure("Value must be non-negative"));
    }

    public ValueTask<Result<int>> HandleAsync(ResultRValidatedRequest request, CancellationToken cancellationToken)
    {
        return new(Result<int>.Success(request.Value * 2));
    }
}

// Request with all hooks
public record ResultRFullPipelineRequest(int Value) : IRequest<int>;

public class ResultRFullPipelineHandler : IRequestHandler<ResultRFullPipelineRequest, int>
{
    public ValueTask<Result> ValidateAsync(ResultRFullPipelineRequest request)
    {
        return new(Result.Success());
    }

    public ValueTask OnPreHandleAsync(ResultRFullPipelineRequest request)
    {
        // Pre-handle hook
        return default;
    }

    public ValueTask<Result<int>> HandleAsync(ResultRFullPipelineRequest request, CancellationToken cancellationToken)
    {
        return new(Result<int>.Success(request.Value * 2));
    }

    public ValueTask OnPostHandleAsync(ResultRFullPipelineRequest request, Result<int> result)
    {
        // Post-handle hook
        return default;
    }
}
