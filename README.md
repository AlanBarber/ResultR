# ResultR

A lightweight, opinionated C# mediator library focused on simplicity and clean design.

## Overview

ResultR provides a minimal yet powerful mediator pattern implementation with built-in result handling, validation, and request lifecycle hooks. It's designed as a modern alternative to MediatR with a smaller surface area and a clearer result pattern.

## Key Features

- **Single Interface Pattern**: Uses only `IRequest<TResponse>` and `IRequestHandler<TRequest, TResponse>` - no distinction between commands and queries
- **Unified Result Type**: All operations return `Result<T>` or `Result`, supporting success/failure states, exception capture, and optional metadata
- **Optional Inline Hooks**: Handlers can implement `Validate()`, `OnPreHandle()`, and `OnPostHandle()` methods without requiring base classes or separate interfaces
- **Request-Specific Logging**: Built-in support for per-request logging via `ILoggerFactory`
- **Minimal Configuration**: Simple DI integration with minimal setup
- **Strong Typing**: Full type safety throughout the pipeline

## Design Philosophy

ResultR prioritizes:
- **Simplicity over flexibility**: Opinionated design choices reduce boilerplate
- **Clean architecture**: No magic strings, reflection-heavy operations, or hidden behaviors
- **Explicit over implicit**: Clear pipeline execution with predictable behavior
- **Modern C# practices**: Leverages latest language features and patterns

## Pipeline Execution

Each request flows through a simple, predictable pipeline:

1. **Validation** - Calls `Validate()` if present, short-circuits on failure
2. **Pre-Handle** - Invokes `OnPreHandle()` for optional logging or setup
3. **Handle** - Executes the core `Handle()` logic
4. **Post-Handle** - Invokes `OnPostHandle()` for logging or cleanup

## Installation

```bash
dotnet add package ResultR
```

## Quick Start

### 1. Define a Request

```csharp
public record CreateUserRequest(string Email, string Name) : IRequest<User>;
```

### 2. Create a Handler

```csharp
public class CreateUserHandler : IRequestHandler<CreateUserRequest, User>
{
    private readonly IUserRepository _repository;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(IUserRepository repository, ILoggerFactory loggerFactory)
    {
        _repository = repository;
        _logger = loggerFactory.CreateLogger<CreateUserHandler>();
    }

    // Optional: Validate the request
    public Result Validate(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result.Failure("Email is required");
        
        if (!request.Email.Contains("@"))
            return Result.Failure("Invalid email format");
        
        return Result.Success();
    }

    // Optional: Pre-handle hook
    public void OnPreHandle(CreateUserRequest request)
    {
        _logger.LogInformation("Creating user with email: {Email}", request.Email);
    }

    // Required: Core handler logic
    public async Task<Result<User>> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = new User(request.Email, request.Name);
            await _repository.AddAsync(user, cancellationToken);
            return Result<User>.Success(user);
        }
        catch (Exception ex)
        {
            return Result<User>.Failure("Failed to create user", ex);
        }
    }

    // Optional: Post-handle hook
    public void OnPostHandle(CreateUserRequest request, Result<User> result)
    {
        if (result.IsSuccess)
            _logger.LogInformation("User created successfully: {UserId}", result.Value.Id);
        else
            _logger.LogError("User creation failed: {Error}", result.Error);
    }
}
```

### 3. Register with DI

```csharp
services.AddResultR(typeof(Program).Assembly);
```

### 4. Send Requests

```csharp
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var result = await _mediator.Send(request);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : BadRequest(result.Error);
    }
}
```

## Result Type

The `Result<T>` type provides a clean way to handle success and failure states:

```csharp
// Success
var success = Result<User>.Success(user);

// Failure with message
var failure = Result<User>.Failure("User not found");

// Failure with exception
var error = Result<User>.Failure("Database error", exception);

// Checking results
if (result.IsSuccess)
{
    var value = result.Value;
}
else
{
    var error = result.Error;
    var exception = result.Exception;
}
```

For void operations, use the non-generic `Result`:

```csharp
public record DeleteUserRequest(Guid UserId) : IRequest<Result>;

public async Task<Result> Handle(DeleteUserRequest request, CancellationToken cancellationToken)
{
    await _repository.DeleteAsync(request.UserId);
    return Result.Success();
}
```

## Advanced Features

### Metadata Support

```csharp
var result = Result<User>.Success(user)
    .WithMetadata("CreatedAt", DateTime.UtcNow)
    .WithMetadata("Source", "API");
```

### Validation Only

```csharp
// Handlers can implement validation without other hooks
public class ValidatingHandler : IRequestHandler<MyRequest, MyResponse>
{
    public Result Validate(MyRequest request)
    {
        // Validation logic
        return Result.Success();
    }

    public async Task<Result<MyResponse>> Handle(MyRequest request, CancellationToken cancellationToken)
    {
        // Handle logic
    }
}
```

## Benchmarks

Performance comparison between ResultR, MediatR 12.5.0, and DispatchR.Mediator 2.1.1:

| Method                        | Mean      | Allocated | Ratio |
|------------------------------ |----------:|----------:|------:|
| DispatchR - Full Pipeline     |  32.95 ns |       0 B |  0.40 |
| DispatchR - Simple            |  37.93 ns |       0 B |  0.46 |
| DispatchR - With Validation   |  38.85 ns |       0 B |  0.48 |
| MediatR - With Validation     |  68.36 ns |     296 B |  0.84 |
| MediatR - Full Pipeline       |  71.22 ns |     296 B |  0.87 |
| MediatR - Simple              |  81.66 ns |     296 B |  1.00 |
| ResultR - With Validation     | 243.02 ns |     592 B |  2.97 |
| ResultR - Simple              | 250.40 ns |     544 B |  3.07 |
| ResultR - Full Pipeline       | 272.63 ns |     592 B |  3.34 |

> **Note**: DispatchR achieves zero allocations through aggressive optimization. ResultR uses compiled expression delegates (cached on first use) to invoke optional lifecycle hooks. While ~3x slower than MediatR, the sub-microsecond difference is negligible for most applications.

Run benchmarks locally:
```bash
cd src/ResultR.Benchmarks
dotnet run -c Release
```

## Why ResultR?

### vs MediatR

- **Simpler**: No pipeline behaviors, notifications, or stream support - just requests and handlers
- **Opinionated**: Built-in validation and lifecycle hooks without configuration
- **Result-focused**: Every operation returns a Result type for consistent error handling
- **Smaller**: Minimal API surface area and dependencies

### vs Custom Implementation

- **Battle-tested patterns**: Proven mediator implementation
- **DI integration**: Automatic handler registration and resolution
- **Type safety**: Compile-time guarantees for request/response matching
- **Extensibility**: Optional hooks without forcing inheritance

## Requirements

- .NET 10.0 or later
- C# 14.0 or later

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

ISC License - see LICENSE file for details

## Roadmap

- [x] Core mediator implementation
- [x] Result types with metadata support
- [x] DI registration extensions
- [x] Comprehensive unit tests
- [x] Performance benchmarks
- [x] NuGet package publication

## Support

- **Issues**: [GitHub Issues](https://github.com/AlanBarber/ResultR/issues)

---

Built with ❤️ for clean, maintainable C# applications.
