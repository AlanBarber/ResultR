# ResultR Documentation

Welcome to the ResultR wiki! This documentation covers everything you need to know to use ResultR effectively in your .NET applications.

## What is ResultR?

ResultR is a lightweight request/response dispatcher for .NET. It routes requests to handlers and wraps all responses in a `Result<T>` type for consistent success/failure handling.

```
Request → Dispatcher → Handler → Result<T>
```

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Request** | A simple class/record implementing `IRequest<TResponse>` (or `IRequest` for void operations) that carries data to a handler |
| **Handler** | A class implementing `IRequestHandler<TRequest, TResponse>` (or `IRequestHandler<TRequest>` for void operations) that processes requests |
| **Dispatcher** | Routes requests to their corresponding handlers via dependency injection |
| **Result** | A wrapper type that represents either success (with a value) or failure (with an error) |

## Pipeline

Every request flows through a predictable pipeline:

```
Validate → BeforeHandle → Handle → AfterHandle
    ↓           ↓           ↓          ↓
 (optional)  (optional)  (required)  (optional)
```

- **ValidateAsync** - Return `Result.Failure()` to short-circuit before handling
- **BeforeHandleAsync** - Run setup logic (logging, etc.)
- **HandleAsync** - Your core business logic
- **AfterHandleAsync** - Run cleanup logic (logging, metrics, etc.)

## Quick Example

```csharp
// 1. Define a request
public record GetUserRequest(int Id) : IRequest<User>;

// 2. Create a handler
public class GetUserHandler : IRequestHandler<GetUserRequest, User>
{
    public async ValueTask<Result<User>> HandleAsync(
        GetUserRequest request, 
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return user is not null 
            ? Result<User>.Success(user)
            : Result<User>.Failure("User not found");
    }
}

// 3. Dispatch the request
var result = await _dispatcher.Dispatch(new GetUserRequest(123));

if (result.IsSuccess)
    Console.WriteLine($"Found: {result.Value.Name}");
else
    Console.WriteLine($"Error: {result.Error}");
```

## Documentation

### Core Documentation

- [Getting Started](Getting-Started) - Installation and basic setup
- [Requests and Handlers](Requests-and-Handlers) - Creating requests and handlers
- [The Result Type](The-Result-Type) - Working with success and failure states
- [Pipeline Hooks](Pipeline-Hooks) - Validation, before/after handling
- [Dependency Injection](Dependency-Injection) - Registering with DI containers
- [Error Handling](Error-Handling) - Exception handling and failure patterns
- [Best Practices](Best-Practices) - Recommended patterns and conventions

### Optional Packages

- [ResultR.Validation](ResultR.Validation) - Inline validation with fluent API

## Requirements

- .NET 10.0 or later
- C# 14.0 or later

## Links

- [GitHub Repository](https://github.com/AlanBarber/ResultR)
- [NuGet Package](https://www.nuget.org/packages/ResultR)
- [Release Notes](https://github.com/AlanBarber/ResultR/releases)
