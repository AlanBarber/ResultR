using DR = DispatchR.Abstractions.Send;

namespace ResultR.Benchmarks.Requests.DispatchR;

// Simple request/response
public sealed record DispatchRSimpleRequest(int Value) : DR.IRequest<DispatchRSimpleRequest, ValueTask<int>>;

public sealed class DispatchRSimpleHandler : DR.IRequestHandler<DispatchRSimpleRequest, ValueTask<int>>
{
    public ValueTask<int> Handle(DispatchRSimpleRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(request.Value * 2);
    }
}

// Request with pipeline behavior (validation equivalent)
public sealed record DispatchRValidatedRequest(int Value) : DR.IRequest<DispatchRValidatedRequest, ValueTask<int>>;

public sealed class DispatchRValidatedHandler : DR.IRequestHandler<DispatchRValidatedRequest, ValueTask<int>>
{
    public ValueTask<int> Handle(DispatchRValidatedRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(request.Value * 2);
    }
}

// Request with full pipeline (pre/post behaviors)
public sealed record DispatchRFullPipelineRequest(int Value) : DR.IRequest<DispatchRFullPipelineRequest, ValueTask<int>>;

public sealed class DispatchRFullPipelineHandler : DR.IRequestHandler<DispatchRFullPipelineRequest, ValueTask<int>>
{
    public ValueTask<int> Handle(DispatchRFullPipelineRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(request.Value * 2);
    }
}
