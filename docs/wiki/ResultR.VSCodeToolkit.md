# ResultR VS Code Toolkit

Supercharge your ResultR development workflow with instant navigation from requests to handlers and one-click scaffolding of new request/handler pairs. The ResultR VS Code Toolkit is the essential companion extension for developers using the ResultR library.

## Installation

### From VS Code Marketplace

1. Open VS Code
2. Go to **Extensions** (Ctrl+Shift+X)
3. Search for "ResultR"
4. Click **Install**

### From GitHub Releases

1. Download the `.vsix` file from the [releases page](https://github.com/AlanBarber/ResultR/releases)
2. Install via command line: `code --install-extension ResultR.VSCodeToolkit.x.x.x.vsix`

### From Open VSX Registry

The extension is also available on [Open VSX](https://open-vsx.org) for VS Code forks like VSCodium.

## Requirements

- VS Code 1.85.0 or later
- Projects using the [ResultR](https://www.nuget.org/packages/ResultR) library

## Features

### Go to Handler Navigation

Instantly navigate from any `IRequest` or `IRequest<T>` type to its corresponding handler implementation.

#### How to Use

1. Place your cursor on any `IRequest` type (variable, parameter, or class definition)
2. Use one of these methods:
   - **Keyboard**: Press `Ctrl+R, Ctrl+H`
   - **Command Palette**: `ResultR: Go to Handler`
   - **Context Menu**: Right-click and select **"Go to Handler..."**

#### What It Finds

The toolkit searches your entire workspace for handlers that implement:
- `IRequestHandler<TRequest>` for `IRequest` types
- `IRequestHandler<TRequest, TResponse>` for `IRequest<TResponse>` types

It works across projects, so your handler can be in a completely different folder or project.

#### Example

```csharp
// Place cursor on HelloWorldRequest and press Ctrl+R, Ctrl+H
var request = new HelloWorldRequest();
var result = await dispatcher.Dispatch(request);
```

The toolkit will navigate to:

```csharp
public class HelloWorldHandler : IRequestHandler<HelloWorldRequest>
{
    public async ValueTask<Result> HandleAsync(HelloWorldRequest request, CancellationToken cancellationToken)
    {
        // Handler implementation
    }
}
```

### Scaffold Request/Handler Pairs

Quickly create new request and handler classes with proper namespaces and structure.

#### How to Use

1. In the **Explorer**, right-click on a folder
2. Select **"ResultR: New Request/Handler"**
3. Enter the request name (e.g., "HelloWorld")
4. Press Enter

Or use the Command Palette:
1. Press `Ctrl+Shift+P`
2. Type "ResultR: New Request/Handler"
3. Select the target folder
4. Enter the request name

#### What Gets Generated

The toolkit creates a single `.cs` file containing both the request and handler:

```csharp
using ResultR;

namespace HelloWorldApp;

public record HelloWorldRequest() : IRequest;

public class HelloWorldHandler : IRequestHandler<HelloWorldRequest>
{
    public async ValueTask<Result> HandleAsync(HelloWorldRequest request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
```

#### Smart Namespace Detection

The toolkit automatically:
- Detects your project's namespace conventions from existing files
- Uses file-scoped namespaces if your project uses them
- Uses block-scoped namespaces if that's your convention
- Derives the namespace from the folder structure and project name

## Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Go to Handler | `Ctrl+R, Ctrl+H` |

## Commands

| Command | Description |
|---------|-------------|
| `ResultR: Go to Handler` | Navigate to the handler for the request under cursor |
| `ResultR: New Request/Handler` | Create a new request/handler pair |

## Troubleshooting

### "No handler found" message

This can happen if:
- The handler doesn't exist yet
- The handler is in a folder excluded from search
- The handler doesn't implement the correct interface

### Navigation goes to wrong handler

Ensure your handler implements the exact interface for your request type. For example:
- `GetUserRequest : IRequest<User>` should have a handler implementing `IRequestHandler<GetUserRequest, User>`

### Scaffold command not working

- Make sure you have a folder selected or right-clicked
- Ensure the workspace contains C# files for namespace detection

## Configuration

The extension works out of the box with sensible defaults. It automatically excludes common folders like `bin`, `obj`, and `node_modules` from searches.

## Links

- [ResultR on NuGet](https://www.nuget.org/packages/ResultR)
- [ResultR.Validation on NuGet](https://www.nuget.org/packages/ResultR.Validation)
- [VS Code Marketplace](https://marketplace.visualstudio.com/items?itemName=AlanBarber.ResultR-VSCodeToolkit)
