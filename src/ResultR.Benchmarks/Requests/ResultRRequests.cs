namespace ResultR.Benchmarks.Requests;

// Simple request/response
public record ResultRSimpleRequest(int Value) : IRequest<int>;

public class ResultRSimpleHandler : IRequestHandler<ResultRSimpleRequest, int>
{
    public Task<Result<int>> Handle(ResultRSimpleRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<int>.Success(request.Value * 2));
    }
}

// Request with validation
public record ResultRValidatedRequest(int Value) : IRequest<int>;

public class ResultRValidatedHandler : IRequestHandler<ResultRValidatedRequest, int>
{
    public Result Validate(ResultRValidatedRequest request)
    {
        return request.Value >= 0 ? Result.Success() : Result.Failure("Value must be non-negative");
    }

    public Task<Result<int>> Handle(ResultRValidatedRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<int>.Success(request.Value * 2));
    }
}

// Request with all hooks
public record ResultRFullPipelineRequest(int Value) : IRequest<int>;

public class ResultRFullPipelineHandler : IRequestHandler<ResultRFullPipelineRequest, int>
{
    public Result Validate(ResultRFullPipelineRequest request)
    {
        return Result.Success();
    }

    public void OnPreHandle(ResultRFullPipelineRequest request)
    {
        // Pre-handle hook
    }

    public Task<Result<int>> Handle(ResultRFullPipelineRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<int>.Success(request.Value * 2));
    }

    public void OnPostHandle(ResultRFullPipelineRequest request, Result<int> result)
    {
        // Post-handle hook
    }
}
