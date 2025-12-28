# 🎯 ResultR

[![GitHub Release](https://img.shields.io/github/v/release/AlanBarber/ResultR)](https://github.com/AlanBarber/ResultR/releases)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/AlanBarber/ResultR/build.yml)](https://github.com/AlanBarber/ResultR/actions/workflows/build.yml)
[![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/AlanBarber/ResultR/total?label=github%20downloads)](https://github.com/AlanBarber/ResultR/releases)
[![NuGet Version](https://img.shields.io/nuget/v/ResultR)](https://www.nuget.org/packages/ResultR)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ResultR?label=nuget%20downloads)](https://www.nuget.org/packages/ResultR)
[![GitHub License](https://img.shields.io/github/license/alanbarber/ResultR)](https://github.com/AlanBarber/ResultR/blob/main/LICENSE)

## 📖 Overview

ResultR is a lightweight request/response dispatcher for .NET applications. It routes requests to handlers and wraps all responses in a `Result<T>` type for consistent success/failure handling.

**What it does:**
- Decouples your application logic by routing requests to dedicated handler classes
- Provides a predictable pipeline: Validate → BeforeHandle → Handle → AfterHandle
- Catches exceptions automatically and returns them as failure results
- Eliminates the need for try/catch blocks scattered throughout your codebase

**What it doesn't do:**
- No notifications or pub/sub messaging
- No pipeline behaviors or middleware chains
- No stream handling

This focused scope keeps the library small, fast, and easy to understand.

## ✨ Key Features

- 🔌 **Simple Interface Pattern**: Uses `IRequest`/`IRequest<TResponse>` and `IRequestHandler<TRequest>`/`IRequestHandler<TRequest, TResponse>` - no distinction between commands and queries
- 📦 **Unified Result Type**: All operations return `Result` or `Result<T>`, supporting success/failure states, exception capture, and optional metadata
- 🪝 **Optional Inline Hooks**: Handlers can override `ValidateAsync()`, `BeforeHandleAsync()`, and `AfterHandleAsync()` methods without requiring base classes or separate interfaces
- ⚡ **Minimal Configuration**: Simple DI integration with minimal setup
- 🔒 **Strong Typing**: Full type safety throughout the pipeline

##  Pipeline Execution

Each request flows through a simple, predictable pipeline:

1. ✅ **Validation** - Calls `ValidateAsync()` if overridden, short-circuits on failure
2. 🚀 **Before Handle** - Invokes `BeforeHandleAsync()` for optional logging or setup
3. ⚙️ **Handle** - Executes the core `HandleAsync()` logic
4. 🏁 **After Handle** - Invokes `AfterHandleAsync()` for logging or cleanup
5. 🛡️ **Exception Handling** - Any exceptions are caught and returned as `Result.Failure` with the exception attached

📚 **[Read the full documentation on the Wiki →](https://github.com/AlanBarber/ResultR/wiki)**

## 💡 Design Philosophy

ResultR prioritizes:
- **Simplicity over flexibility**: Opinionated design choices reduce boilerplate
- **Clean architecture**: No magic strings, reflection-heavy operations, or hidden behaviors
- **Explicit over implicit**: Clear pipeline execution with predictable behavior
- **Modern C# practices**: Leverages latest language features and patterns

## 📥 Installation

```bash
dotnet add package ResultR
```

### Optional: ResultR.Validation

For inline validation with a fluent API:

```bash
dotnet add package ResultR.Validation
```

**[→ Learn more about ResultR.Validation](https://github.com/AlanBarber/ResultR/wiki/ResultR.Validation)**

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

    public CreateUserHandler(IUserRepository repository, ILogger<CreateUserHandler> logger)
    {
        _repository = repository;
        _logger = logger;
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

For void operations, use the non-generic `Result` with `IRequest`:

```csharp
public record DeleteUserRequest(Guid UserId) : IRequest;

public class DeleteUserHandler : IRequestHandler<DeleteUserRequest>
{
    public async ValueTask<Result> HandleAsync(DeleteUserRequest request, CancellationToken cancellationToken)
    {
        await _repository.DeleteAsync(request.UserId);
        return Result.Success();
    }
}
```

## 🔧 Advanced Features

### ResultR.Validation (Optional Package)

Add fluent inline validation to your handlers without separate validator classes:

```csharp
dotnet add package ResultR.Validation
```

```csharp
using ResultR.Validation;

public class CreateUserHandler : IRequestHandler<CreateUserRequest, User>
{
    public ValueTask<Result> ValidateAsync(CreateUserRequest request)
    {
        return Validator.For(request)
            .RuleFor(x => x.Email)
                .NotEmpty("Email is required")
                .EmailAddress("Invalid email format")
            .RuleFor(x => x.Name)
                .NotEmpty("Name is required")
                .MinLength(2, "Name must be at least 2 characters")
            .RuleFor(x => x.Age)
                .GreaterThan(0, "Age must be positive")
            .ToResult();
    }

    public async ValueTask<Result<User>> HandleAsync(CreateUserRequest request, CancellationToken ct)
    {
        var user = new User(request.Email, request.Name, request.Age);
        await _repository.AddAsync(user, ct);
        return Result<User>.Success(user);
    }
}
```

**[→ Full ResultR.Validation documentation](https://github.com/AlanBarber/ResultR/wiki/ResultR.Validation)**

### Metadata Support

```csharp
var result = Result<User>.Success(user)
    .WithMetadata("CreatedAt", DateTime.UtcNow)
    .WithMetadata("Source", "API");
```

### Optional Hooks

Override only the hooks you need - no base class required:

```csharp
// Just validation + handle (no before/after hooks)
public class ValidatingHandler : IRequestHandler<CreateOrderRequest, Order>
{
    public ValueTask<Result> ValidateAsync(CreateOrderRequest request)
    {
        if (request.Items.Count == 0)
            return new(Result.Failure("Order must have at least one item"));
        
        return new(Result.Success());
    }

    public async ValueTask<Result<Order>> HandleAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        // This only runs if validation passes
        var order = await _repository.CreateAsync(request, cancellationToken);
        return Result<Order>.Success(order);
    }
}
```

## ❓ FAQ

### Why "Dispatcher" instead of "Mediator"?

The classic GoF Mediator pattern describes an object that coordinates bidirectional communication between multiple colleague objects - think of a chat room where participants talk *through* the mediator to each other.

What ResultR actually does is simpler: route a request to exactly one handler and return a response. There's no inter-handler communication. This is closer to a **command pattern** or **in-process message bus**.

We chose `IDispatcher` and `Dispatcher` because the name honestly describes the behavior: requests go in, get dispatched to a handler, and results come out.

## 📊 Benchmarks

There are many great request dispatcher / "mediator" implementations out there. Here is a comparison between ResultR and some of the other popular ones:

Performance comparison between ResultR (latest), [MediatR](https://github.com/jbogard/MediatR) (12.5.0), [DispatchR](https://github.com/hasanxdev/DispatchR) (2.1.1), and [Mediator.SourceGenerator](https://github.com/martinothamar/Mediator) (2.1.7):

| Method                        | Mean      | Allocated | Ratio |
|------------------------------ |----------:|----------:|------:|
| MediatorSG - With Validation  |  20.26 ns |      72 B |  0.27 |
| MediatorSG - Simple           |  23.01 ns |      72 B |  0.31 |
| DispatchR - With Validation   |  31.37 ns |      96 B |  0.42 |
| DispatchR - Simple            |  34.93 ns |      96 B |  0.47 |
| DispatchR - Full Pipeline     |  44.02 ns |      96 B |  0.59 |
| MediatorSG - Full Pipeline    |  44.35 ns |      72 B |  0.59 |
| ResultR - Full Pipeline       |  62.92 ns |     264 B |  0.84 |
| MediatR - Simple              |  75.03 ns |     296 B |  1.00 |
| ResultR - With Validation     |  77.10 ns |     264 B |  1.03 |
| ResultR - Simple              |  95.42 ns |     264 B |  1.27 |
| MediatR - With Validation     | 120.28 ns |     608 B |  1.60 |
| MediatR - Full Pipeline       | 158.01 ns |     824 B |  2.11 |

> **Note on benchmark methodology:** All libraries are configured with equivalent pipeline behaviors (validation, pre/post processing) for fair comparison. MediatorSG and DispatchR use source generation for optimal performance. ResultR always executes its full pipeline (Validate → BeforeHandle → Handle → AfterHandle) even when hooks use default implementations, which explains why "Simple" is slower than "Full Pipeline" - they're doing the same work.

> **What does this mean?** When comparing equivalent functionality (full pipeline with behaviors), ResultR (63ns) significantly outperforms MediatR (158ns) - over 2.5x faster. The source-generated libraries (MediatorSG, DispatchR) are fastest but require compile-time code generation. In real applications where database queries take 1-10ms and HTTP calls take 50-500ms, these nanosecond differences are negligible. ResultR also allocates less memory than MediatR (264B vs 296-824B), reducing GC pressure in high-throughput scenarios.

Run benchmarks locally:
```bash
cd src/ResultR.Benchmarks
dotnet run -c Release
```

## 📋 Requirements

- .NET 10.0 or later
- C# 14.0 or later

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

ISC License - see the [LICENSE](https://github.com/AlanBarber/ResultR/blob/main/LICENSE) file for details.

## 💬 Support

- **Issues**: [GitHub Issues](https://github.com/AlanBarber/ResultR/issues)

---

Built with ❤️ for the C# / DotNet community.
