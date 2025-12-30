# ResultR Feature Plan

This document tracks planned features for the ResultR ecosystem. Each feature is designed to enhance the developer experience when working with ResultR.

---

## Prerequisite: Independent Release Workflows

**Status:** 📋 Planned  
**Priority:** Critical (must complete before any feature releases)

### Overview

Set up independent GitHub Actions workflows for each package/extension to enable separate versioning and releases. Each component will have its own workflow file triggered by a unique tag prefix.

### Tag Convention

| Component | Tag Prefix | Example |
|-----------|------------|---------|
| ResultR (core) | `v*` | `v1.0.0` |
| ResultR.Validation | `validation-v*` | `validation-v1.0.0` |
| VS Extension | `vs-v*` | `vs-v1.0.0` |
| VS Code Extension | `vscode-v*` | `vscode-v1.0.0` |
| Rider/ReSharper Extension | `rider-v*` | `rider-v1.0.0` |

### Workflow Files

| File | Trigger | Publishes To |
|------|---------|--------------|
| `.github/workflows/release-resultr.yml` | `v*` tags | NuGet |
| `.github/workflows/release-validation.yml` | `validation-v*` tags | NuGet |
| `.github/workflows/release-vs.yml` | `vs-v*` tags | VS Marketplace |
| `.github/workflows/release-vscode.yml` | `vscode-v*` tags | VS Code Marketplace |
| `.github/workflows/release-rider.yml` | `rider-v*` tags | JetBrains Marketplace |

### Benefits

- **Independent versioning** - Each package can follow its own semantic versioning
- **Targeted releases** - Only publish what actually changed
- **Clear release history** - GitHub releases are organized by component
- **Simpler CI/CD** - Each workflow is focused and maintainable

### Tasks

- [ ] Rename `build.yml` to `release-resultr.yml` (keep existing `v*` trigger)
- [ ] Create `release-validation.yml` for ResultR.Validation
- [ ] Create `release-vs.yml` for Visual Studio extension
- [ ] Create `release-vscode.yml` for VS Code extension
- [ ] Create `release-rider.yml` for Rider/ReSharper extension
- [ ] Document release process in README or CONTRIBUTING.md

---

## Feature 1: ResultR.Validation

**Status:** ✅ Completed  
**Package:** `ResultR.Validation` (separate NuGet package)  
**Priority:** High  
**Released:** December 2024

### Overview

A lightweight, inline validation framework designed specifically for ResultR's `ValidateAsync` pipeline hook. Unlike FluentValidation which requires separate validator classes, this framework allows you to define validation rules **directly inside your `ValidateAsync()` method** using a fluent API. It integrates directly with ResultR's `Result` type, automatically returning `Result.Success()` or `Result.Failure()` with aggregated validation errors.

### Goals

- **Zero ceremony** - No external validator classes, no DI registration for validators
- Define validation rules inline within `ValidateAsync()` using a fluent API
- Seamlessly integrate with `IRequestHandler<TRequest>.ValidateAsync()` and `IRequestHandler<TRequest, TResponse>.ValidateAsync()`
- Return `Result.Success()` when all validations pass
- Return `Result.Failure()` with aggregated error messages when any validation fails
- Support core validation rules out of the box (NotEmpty, NotNull, Length, Range, etc.)
- Allow custom validation rules via `Must()` predicate

### Proposed API

```csharp
public record CreateUserRequest(string Email, string Name, int Age) : IRequest<User>;

public class CreateUserHandler : IRequestHandler<CreateUserRequest, User>
{
    public ValueTask<Result> ValidateAsync(CreateUserRequest request)
    {
        // Inline validation - no external validator class needed!
        return Validator.For(request)
            .RuleFor(x => x.Email)
                .NotEmpty("Email is required")
                .EmailAddress("Invalid email format")
            .RuleFor(x => x.Name)
                .NotEmpty("Name is required")
                .MinLength(2, "Name must be at least 2 characters")
                .MaxLength(100, "Name cannot exceed 100 characters")
            .RuleFor(x => x.Age)
                .GreaterThan(0, "Age must be positive")
                .LessThanOrEqualTo(150, "Age must be realistic")
            .ToResult();
    }

    public async ValueTask<Result<User>> HandleAsync(CreateUserRequest request, CancellationToken ct)
    {
        var user = new User(request.Email, request.Name);
        // Save user...
        return Result<User>.Success(user);
    }
}
```

### Custom Validation Rules

```csharp
public ValueTask<Result> ValidateAsync(CreateUserRequest request)
{
    return Validator.For(request)
        .RuleFor(x => x.Email)
            .NotEmpty("Email is required")
            .Must(email => email.EndsWith("@company.com"), "Must use company email")
        .RuleFor(x => x.Name)
            .Must(name => !name.Contains("admin", StringComparison.OrdinalIgnoreCase), 
                  "Name cannot contain 'admin'")
        .ToResult();
}
```

### Core Components

| Component | Description |
|-----------|-------------|
| `Validator` | Static entry point with `For<T>(T instance)` method |
| `ValidationBuilder<T>` | Fluent builder for chaining `RuleFor()` calls |
| `RuleBuilder<T, TProperty>` | Fluent builder for chaining validation rules on a property |

### Built-in Validation Rules

- **String:** `NotEmpty`, `NotNull`, `MinLength`, `MaxLength`, `Length`, `Matches` (regex)
- **Numeric:** `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `Between`
- **Collections:** `NotEmpty`, `MinCount`, `MaxCount`
- **General:** `NotNull`, `NotEqual`, `Equal`, `Must` (custom predicate)

### How It Works

1. `Validator.For(request)` creates a `ValidationBuilder<T>` for the request
2. Each `RuleFor()` or validation method adds a rule to an internal list
3. `ToResult()` executes all rules and returns:
   - `Result.Success()` if all rules pass
   - `Result.Failure("Validation failed")` with errors stored in metadata

### Validation Result Structure

When validation fails, the `Result` includes:
- **Error message:** `"Validation failed"` (or customizable summary)
- **Metadata key:** `"ValidationErrors"` containing a `List<ValidationError>`

```csharp
// ValidationError record
public record ValidationError(string PropertyName, string ErrorMessage);

// Accessing validation errors from a failed result
if (result.IsFailure)
{
    var validationErrors = result.GetMetadataValueOrDefault<List<ValidationError>>("ValidationErrors");
    if (validationErrors is not null)
    {
        foreach (var error in validationErrors)
        {
            Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
        }
    }
}

// Example output:
// Email: Email is required
// Name: Name must be at least 2 characters
// Age: Age must be positive
```

### Tasks

- [x] Create `ResultR.Validation` project structure
- [x] Implement `Validator` static class with `For<T>()` entry point
- [x] Implement `ValidationBuilder<T>` for building validation chains
- [x] Implement `RuleBuilder<T, TProperty>` for property-specific rules
- [x] Implement built-in validation rules as extension methods
- [x] Implement `ToResult()` method returning `ValueTask<Result>`
- [x] Write unit tests (78 tests passing)
- [x] Write documentation and examples
- [x] Publish NuGet package

### Deliverables

- **Package:** `ResultR.Validation` on NuGet.org
- **Source:** `src/ResultR.Validation/` (8 files, ~1,500 LOC)
- **Tests:** `src/ResultR.Validation.Tests/` (9 test files, 78 passing tests)
- **Documentation:**
  - Package README: `src/ResultR.Validation/README.md`
  - Wiki documentation: `docs/wiki/ResultR.Validation.md`
  - Main README integration
- **CI/CD:** GitHub Actions workflow (`release-validation.yml`)
- **Built-in Rules:** 16+ validation methods (string, numeric, collection, general)
- **XML Documentation:** Complete IntelliSense support

---

## Feature 2: Visual Studio Extension

**Status:** ✅ Implemented  
**Package:** `ResultR.VisualStudio` (VSIX)  
**Priority:** Medium

### Overview

A Visual Studio extension that provides quick navigation from `IRequest`/`IRequest<T>` classes to their corresponding `IRequestHandler` implementations.

### Features

- **Context Menu:** Right-click on any class implementing `IRequest` or `IRequest<T>` to see "Go to Handler" option
- **Keyboard Shortcut:** `Ctrl+Shift+H` to navigate from request to handler
- **Smart Detection:** Automatically detects the handler type based on the request interface
- **Multi-Handler Support:** If multiple handlers exist (shouldn't happen, but edge case), show a message

### Technical Approach

1. Use Roslyn to analyze the current document and find the class under cursor
2. Check if the class implements `IRequest` or `IRequest<T>`
3. Search the solution for classes implementing `IRequestHandler<TRequest>` or `IRequestHandler<TRequest, TResponse>` where `TRequest` matches
4. Navigate to the handler file and position cursor at the class declaration

### Implementation Details

- **Target:** Visual Studio 2022 (17.0+)
- **Framework:** .NET Framework 4.8
- **SDK:** Visual Studio SDK (VSIX)
- **APIs:** Roslyn Workspace APIs, `IVsTextManager`, `IVsUIShellOpenDocument`
- **Location:** `src/ResultR.VisualStudio/`

### Tasks

- [x] Create VSIX project structure
- [x] Implement Roslyn-based request/handler detection
- [x] Add context menu command
- [x] Register `Ctrl+Shift+H` keyboard shortcut
- [x] Handle edge cases (no handler found, multiple handlers)
- [x] Build successfully
- [ ] Add settings/options page (future enhancement)
- [ ] Test with various solution structures
- [ ] Publish to Visual Studio Marketplace

---

## Feature 3: Visual Studio Code Extension

**Status:** 📋 Planned  
**Package:** `resultr-vscode` (VS Code Extension)  
**Priority:** Medium

### Overview

A Visual Studio Code extension providing the same request-to-handler navigation as the Visual Studio extension.

### Features

- **Context Menu:** Right-click on `IRequest`/`IRequest<T>` class → "ResultR: Go to Handler"
- **Keyboard Shortcut:** `Ctrl+Shift+H` (Windows/Linux) / `Cmd+Shift+H` (macOS)
- **Command Palette:** "ResultR: Navigate to Handler"
- **CodeLens:** Show "Go to Handler" link above request class declarations

### Technical Approach

1. Use the C# extension's language server or implement custom parsing
2. Parse the current file to identify request classes
3. Search workspace for matching handler implementations
4. Use VS Code's `vscode.window.showTextDocument` to navigate

### Implementation Details

- **Language:** TypeScript
- **APIs:** VS Code Extension API, potentially OmniSharp/C# DevKit integration
- **Activation:** On C# file open

### Tasks

- [ ] Create VS Code extension project structure (`yo code`)
- [ ] Implement C# file parsing for request detection
- [ ] Implement workspace search for handlers
- [ ] Add context menu contribution
- [ ] Register keyboard shortcut
- [ ] Add CodeLens provider (optional enhancement)
- [ ] Add extension settings
- [ ] Test with various workspace configurations
- [ ] Publish to VS Code Marketplace

---

## Feature 4: JetBrains Rider / ReSharper Extension

**Status:** 📋 Planned  
**Package:** `ResultR.Rider` (JetBrains Plugin)  
**Priority:** Medium

### Overview

A JetBrains plugin for Rider and ReSharper providing request-to-handler navigation, consistent with the Visual Studio and VS Code extensions.

### Features

- **Context Menu:** Right-click on `IRequest`/`IRequest<T>` class → "Go to Handler"
- **Keyboard Shortcut:** `Ctrl+Shift+H`
- **Navigation Gutter Icon:** Click icon in gutter to navigate to handler
- **Find Usages Integration:** Show handler in "Find Usages" results for request types

### Technical Approach

1. Use ReSharper SDK's PSI (Program Structure Interface) for code analysis
2. Implement `INavigateFromHereProvider` for navigation
3. Use `IDeclaredElement` to find request and handler types
4. Leverage existing "Go to Implementation" patterns

### Implementation Details

- **SDK:** JetBrains ReSharper SDK / Rider SDK
- **Language:** C#
- **Target:** Rider 2023.3+ / ReSharper 2023.3+

### Tasks

- [ ] Create JetBrains plugin project structure
- [ ] Implement PSI-based request/handler detection
- [ ] Add navigation action
- [ ] Register keyboard shortcut
- [ ] Add gutter icon provider
- [ ] Integrate with Find Usages (optional)
- [ ] Test in both Rider and ReSharper
- [ ] Publish to JetBrains Marketplace

---

## Implementation Priority

| Priority | Feature | Rationale |
|----------|---------|-----------|
| 1 | ResultR.Validation | Core functionality that enhances the library's value proposition |
| 2 | VS Code Extension | Largest potential user base, TypeScript is accessible |
| 3 | Visual Studio Extension | Important for enterprise .NET developers |
| 4 | Rider/ReSharper Extension | Completes IDE coverage for .NET ecosystem |

---

## Notes

- All IDE extensions should share consistent UX (same shortcut, same menu text)
- Consider creating a shared specification document for handler detection logic
- Extensions should gracefully handle cases where ResultR is not referenced in the project
- Future enhancement: Bi-directional navigation (handler → request)

---

*Last Updated: December 2024*
