# ResultR

[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/AlanBarber/ResultR/ci.yml)](https://github.com/AlanBarber/ResultR/actions/workflows/ci.yml)
[![NuGet Version](https://img.shields.io/nuget/v/ResultR)](https://www.nuget.org/packages/ResultR)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ResultR?label=downloads)](https://www.nuget.org/packages/ResultR)
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
- No distributed messaging

This focused scope keeps the library small, fast, and easy to understand.

## 📋 Requirements

- .NET 10.0 or later
- C# 14.0 or later

## 📥 Installation

```bash
dotnet add package ResultR
```

## 🚀 Quick Start

### 1. Define a Request

```csharp
public record CreateUserRequest(string Email, string Name, int Age) : IRequest<User>;
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
        var user = new User(request.Email, request.Name, request.Age);
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

## ❓ Why ResultR?

ResultR prioritizes simplicity and explicitness over flexibility, giving you a focused request/handler pattern without the complexity of pipelines, behaviors, or middleware chains that you may never need. Every operation returns a unified Result or Result<T> type, making error handling consistent and predictable across your entire codebase. The optional inline hooks (ValidateAsync, BeforeHandleAsync, AfterHandleAsync) let you add cross-cutting concerns directly in your handlers without separate classes or DI registrations, keeping related logic together and reducing ceremony.

## 💬 Support

- **Issues**: [GitHub Issues](https://github.com/AlanBarber/ResultR/issues)
- **Documentation**: [GitHub Wiki](https://github.com/AlanBarber/ResultR/wiki)

## 🔗 Links

- [GitHub Repository](https://github.com/AlanBarber/ResultR)
- [ResultR on NuGet](https://www.nuget.org/packages/ResultR)
- [ResultR.Validation on NuGet](https://www.nuget.org/packages/ResultR.Validation)
- [ResultR.VSToolkit on VS Marketplace](https://marketplace.visualstudio.com/items?itemName=AlanBarber.ResultR-VSToolkit)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

ISC License - see the [LICENSE](https://github.com/AlanBarber/ResultR/blob/main/LICENSE) file for details.

---

Built with ❤️ for the C# / DotNet community.
