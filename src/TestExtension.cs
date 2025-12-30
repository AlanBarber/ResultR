using ResultR;

namespace TestExtension;

// Test request - place cursor on "TestRequest" and right-click
public record TestRequest(int Id) : IRequest<string>;

// Test handler for the above request
public class TestRequestHandler : IRequestHandler<TestRequest, string>
{
    public ValueTask<Result<string>> HandleAsync(TestRequest request, CancellationToken cancellationToken)
    {
        return new(Result<string>.Success($"Result for {request.Id}"));
    }
}

// Another test request with no response
public record DeleteTestRequest(int Id) : IRequest;

// Handler for void request
public class DeleteTestHandler : IRequestHandler<DeleteTestRequest>
{
    public ValueTask<Result> HandleAsync(DeleteTestRequest request, CancellationToken cancellationToken)
    {
        return new(Result.Success());
    }
}
