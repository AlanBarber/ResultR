# Getting Started

This guide walks you through installing ResultR and creating your first request/handler pair.

## Installation

Install via NuGet:

```bash
dotnet add package ResultR
```

Or add to your `.csproj`:

```xml
<PackageReference Include="ResultR" Version="1.*" />
```

## Basic Setup

### 1. Register Services

In your `Program.cs` or startup configuration:

```csharp
using ResultR;

var builder = WebApplication.CreateBuilder(args);

// Option 1: Auto-scan entry assembly
builder.Services.AddResultR();

// Option 2: Specify assemblies explicitly (for multi-project solutions)
builder.Services.AddResultR(
    typeof(Program).Assembly,
    typeof(MyHandlers).Assembly);
```

### 2. Create a Request

A request is a simple class or record that implements `IRequest<TResponse>`:

```csharp
public record CreateUserRequest(string Email, string Name) : IRequest<User>;
```

The generic parameter `User` is the type your handler will return on success.

### 3. Create a Handler

A handler implements `IRequestHandler<TRequest, TResponse>`:

```csharp
public class CreateUserHandler : IRequestHandler<CreateUserRequest, User>
{
    private readonly IUserRepository _repository;

    public CreateUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Result<User>> HandleAsync(
        CreateUserRequest request, 
        CancellationToken cancellationToken)
    {
        var user = new User(request.Email, request.Name);
        await _repository.AddAsync(user, cancellationToken);
        return Result<User>.Success(user);
    }
}
```

### 4. Dispatch Requests

Inject `IDispatcher` and use it to send requests:

```csharp
public class UserController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public UserController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        var result = await _dispatcher.Dispatch(request);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : BadRequest(result.Error);
    }
}
```

## What's Next?

- [Requests and Handlers](Requests-and-Handlers) - Learn more about creating requests and handlers
- [The Result Type](The-Result-Type) - Understanding success and failure handling
- [Pipeline Hooks](Pipeline-Hooks) - Add validation and pre/post processing
