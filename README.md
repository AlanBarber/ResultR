# 🎯 ResultR

[![GitHub Release](https://img.shields.io/github/v/release/AlanBarber/ResultR)](https://github.com/AlanBarber/ResultR/releases)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/AlanBarber/ResultR/build.yml)](https://github.com/AlanBarber/ResultR/actions/workflows/build.yml)
[![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/AlanBarber/ResultR/total?label=github%20downloads)](https://github.com/AlanBarber/ResultR/releases)
[![NuGet Version](https://img.shields.io/nuget/v/ResultR)](https://www.nuget.org/packages/ResultR)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ResultR?label=nuget%20downloads)](https://www.nuget.org/packages/ResultR)
[![GitHub License](https://img.shields.io/github/license/alanbarber/ResultR)](https://github.com/AlanBarber/ResultR/blob/main/LICENSE)

A lightweight, opinionated mediator library for C#. Provides a unified IRequest and IRequestHandler pattern with built-in result wrapping, optional validation, and per-request logging.

## 📖 Overview

ResultR provides a minimal yet powerful request/response dispatcher with built-in result handling, validation, and request lifecycle hooks. It's designed as a modern alternative to MediatR with a smaller surface area and a clearer result pattern.

### Why do you call it a Dispatcher? Isn't this just a another Mediator library?

Yes and no. While ResultR serves the same purpose as MediatR and similar libraries, we chose different naming to be more technically accurate. The classic GoF Mediator pattern describes an object that coordinates bidirectional communication between multiple colleague objects - think of a chat room where participants talk *through* the mediator to each other.

What these libraries, and ResultR specifically, actually do is simpler: route a request to exactly one handler and return a response. There's no inter-handler communication. This is closer to a **command pattern** or **in-process message bus** pattern.

We chose `IDispatcher` and `Dispatcher` because the name honestly describes the behavior: requests go in, get dispatched to a handler, and results come out.

## ✨ Key Features

- 🔌 **Single Interface Pattern**: Uses only `IRequest<TResponse>` and `IRequestHandler<TRequest, TResponse>` - no distinction between commands and queries
- 📦 **Unified Result Type**: All operations return `Result<T>` or `Result`, supporting success/failure states, exception capture, and optional metadata
- 🪝 **Optional Inline Hooks**: Handlers can override `ValidateAsync()`, `BeforeHandleAsync()`, and `AfterHandleAsync()` methods without requiring base classes or separate interfaces
- 📝 **Request-Specific Logging**: Built-in support for per-request logging via `ILoggerFactory`
- ⚡ **Minimal Configuration**: Simple DI integration with minimal setup
- 🔒 **Strong Typing**: Full type safety throughout the pipeline

## 💡 Design Philosophy

ResultR prioritizes:
- **Simplicity over flexibility**: Opinionated design choices reduce boilerplate
- **Clean architecture**: No magic strings, reflection-heavy operations, or hidden behaviors
- **Explicit over implicit**: Clear pipeline execution with predictable behavior
- **Modern C# practices**: Leverages latest language features and patterns

## 🔄 Pipeline Execution

Each request flows through a simple, predictable pipeline:

1. ✅ **Validation** - Calls `ValidateAsync()` if overridden, short-circuits on failure
2. 🚀 **Before Handle** - Invokes `BeforeHandleAsync()` for optional logging or setup
3. ⚙️ **Handle** - Executes the core `HandleAsync()` logic
4. 🏁 **After Handle** - Invokes `AfterHandleAsync()` for logging or cleanup
5. 🛡️ **Exception Handling** - Any exceptions are caught and returned as `Result.Failure` with the exception attached

## 📥 Installation

```bash
dotnet add package ResultR
```

## 🚀 Quick Start

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

    // Optional: Validate the request (override virtual method)
    public ValueTask<Result> ValidateAsync(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return new(Result.Failure("Email is required"));
        
        if (!request.Email.Contains("@"))
            return new(Result.Failure("Invalid email format"));
        
        return new(Result.Success());
    }

    // Optional: Before handle hook (override virtual method)
    public ValueTask BeforeHandleAsync(CreateUserRequest request)
    {
        _logger.LogInformation("Creating user with email: {Email}", request.Email);
        return default;
    }

    // Required: Core handler logic
    public async ValueTask<Result<User>> HandleAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        // Exceptions are automatically caught and converted to Result.Failure
        var user = new User(request.Email, request.Name);
        await _repository.AddAsync(user, cancellationToken);
        return Result<User>.Success(user);
    }

    // Optional: After handle hook (override virtual method)
    public ValueTask AfterHandleAsync(CreateUserRequest request, Result<User> result)
    {
        if (result.IsSuccess)
            _logger.LogInformation("User created successfully: {UserId}", result.Value.Id);
        else
            _logger.LogError("User creation failed: {Error}", result.Error);
        return default;
    }
}
```

### 3. Register with DI

```csharp
// Simple: auto-scans entry assembly
services.AddResultR();

// Or explicit: scan specific assemblies (for multi-project solutions)
services.AddResultR(
    typeof(Program).Assembly,
    typeof(MyHandlers).Assembly);
```

### 4. Dispatch Requests

```csharp
public class UserController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public UserController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(CreateUserRequest request)
    {
        var result = await _dispatcher.Dispatch(request);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : BadRequest(result.Error);
    }
}
```

## 📦 Result Type

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

public async ValueTask<Result<Result>> HandleAsync(DeleteUserRequest request, CancellationToken cancellationToken)
{
    await _repository.DeleteAsync(request.UserId);
    return Result<Result>.Success(Result.Success());
}
```

## 🔧 Advanced Features

### Metadata Support

```csharp
var result = Result<User>.Success(user)
    .WithMetadata("CreatedAt", DateTime.UtcNow)
    .WithMetadata("Source", "API");
```

### Validation Only

```csharp
// Handlers can override validation without other hooks
public class ValidatingHandler : IRequestHandler<MyRequest, MyResponse>
{
    public ValueTask<Result> ValidateAsync(MyRequest request)
    {
        // Validation logic
        return new(Result.Success());
    }

    public async ValueTask<Result<MyResponse>> HandleAsync(MyRequest request, CancellationToken cancellationToken)
    {
        // Handle logic
    }
}
```

## 📊 Benchmarks

There are many great request dispatcher implementations out there. Here is a comparison between ResultR and some of the other popular ones:

Performance comparison between ResultR (latest), [MediatR](https://github.com/jbogard/MediatR) (12.5.0), [DispatchR](https://github.com/hasanxdev/DispatchR) (2.1.1), and [Mediator.SourceGenerator](https://github.com/martinothamar/Mediator) (2.1.7):

| Method                        | Mean      | Allocated | Ratio |
|------------------------------ |----------:|----------:|------:|
| MediatorSG - With Validation  |  18.68 ns |      72 B |  0.31 |
| MediatorSG - Simple           |  18.87 ns |      72 B |  0.31 |
| MediatorSG - Full Pipeline    |  20.26 ns |      72 B |  0.34 |
| DispatchR - With Validation   |  29.28 ns |      96 B |  0.49 |
| DispatchR - Simple            |  29.58 ns |      96 B |  0.49 |
| DispatchR - Full Pipeline     |  30.15 ns |      96 B |  0.50 |
| MediatR - Full Pipeline       |  59.89 ns |     296 B |  1.00 |
| MediatR - Simple              |  60.02 ns |     296 B |  1.00 |
| MediatR - With Validation     |  62.95 ns |     296 B |  1.05 |
| ResultR - With Validation     |  80.73 ns |     264 B |  1.35 |
| ResultR - Full Pipeline       |  80.97 ns |     264 B |  1.35 |
| ResultR - Simple              |  81.65 ns |     264 B |  1.36 |

> **What does this mean?** The difference between ResultR (~81ns) and MediatR (~60ns) is roughly 20 nanoseconds - that's 0.00002 milliseconds. In real applications where a typical database query takes 1-10ms and HTTP calls take 50-500ms, this difference is completely negligible. ResultR also allocates less memory per request (264B vs 296B), which can reduce garbage collection pressure in high-throughput scenarios.

Run benchmarks locally:
```bash
cd src/ResultR.Benchmarks
dotnet run -c Release
```

## 🤔 Why ResultR?

### vs MediatR

- **Simpler**: No pipeline behaviors, notifications, or stream support - just requests and handlers
- **Opinionated**: Built-in validation and lifecycle hooks without configuration
- **Result-focused**: Every operation returns a Result type for consistent error handling
- **Smaller**: Minimal API surface area and dependencies
- **Honest naming**: Uses `Dispatcher` instead of `Mediator` to accurately describe the pattern

### vs Custom Implementation

- **Battle-tested patterns**: Proven request dispatcher implementation
- **DI integration**: Automatic handler registration and resolution
- **Type safety**: Compile-time guarantees for request/response matching
- **Extensibility**: Optional hooks without forcing inheritance

## 📋 Requirements

- .NET 10.0 or later
- C# 14.0 or later

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

ISC License - see LICENSE file for details

## 🗺️ Roadmap

- [x] Core dispatcher implementation
- [x] Result types with metadata support
- [x] DI registration extensions
- [x] Comprehensive unit tests
- [x] Performance benchmarks
- [x] NuGet package publication

## 💬 Support

- **Issues**: [GitHub Issues](https://github.com/AlanBarber/ResultR/issues)

---

Built with ❤️ for clean, maintainable C# applications.
