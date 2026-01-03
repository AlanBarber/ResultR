# ResultR VS Code Toolkit

Supercharge your [ResultR](https://github.com/AlanBarber/ResultR) development workflow with instant navigation from requests to handlers and one-click scaffolding of new request/handler pairs. The ResultR VS Code Toolkit is the essential companion extension for developers using the [ResultR](https://github.com/AlanBarber/ResultR) library.

## Features

### ⚡Navigate Your Codebase Instantly 

Tired of hunting through your workspace to find the handler for a request? The ResultR VS Code Toolkit makes navigation effortless! Simply place your cursor on any IRequest type - whether it's a variable, parameter, or class definition - and press Ctrl+R, Ctrl+H (or right-click and select "Go to Handler..."). The toolkit instantly locates and opens the corresponding IRequestHandler implementation, even if it's in a completely different project. No more manual searching, no more wasted time. Just click and you're there!

### 📝 Scaffold New Request/Handler Pairs

Creating new request/handler pairs has never been easier! Right-click on any folder in the Explorer and select "ResultR: New Request/Handler". Enter your request name, and the toolkit generates a properly structured .cs file with the correct namespace (automatically detecting whether you use file-scoped or block-scoped namespaces), all the necessary using statements, and a ready-to-implement handler class. The generated code follows your project's existing conventions, so it fits right in with your codebase. Spend less time on boilerplate and more time on what matters - your business logic! 

## Requirements

- VS Code 1.85.0 or later
- Projects using the [ResultR](https://github.com/AlanBarber/ResultR) library

## Installation

1. Open VS Code
2. Go to Extensions (Ctrl+Shift+X)
3. Search for "ResultR"
4. Click Install

Or download the `.vsix` file from the [releases page](https://github.com/AlanBarber/ResultR/releases) and install via `code --install-extension <file>.vsix`.

## ❓ Why ResultR VS Code Toolkit?

The ResultR VS Code Toolkit provides a simple way to navigate between `IRequest`/`IRequest<T>` classes and their corresponding `IRequestHandler` implementations. It also provides a scaffold for creating new request/handler pairs. It's the perfect companion to the [ResultR](https://github.com/AlanBarber/ResultR) library!

## 💬 Support

- **Documentation**: [GitHub Wiki](https://github.com/AlanBarber/ResultR/wiki/ResultR.VSCodeToolkit)
- **Issues**: [GitHub Issues](https://github.com/AlanBarber/ResultR/issues)


## 🔗 Links

- [GitHub Repository](https://github.com/AlanBarber/ResultR)
- [ResultR on NuGet](https://www.nuget.org/packages/ResultR)
- [ResultR.Validation on NuGet](https://www.nuget.org/packages/ResultR.Validation)
- [ResultR.VSToolkit on VS Marketplace](https://marketplace.visualstudio.com/items?itemName=AlanBarber.ResultR-VSToolkit)
- [ResultR.VSCodeToolkit on VS Marketplace](https://marketplace.visualstudio.com/items?itemName=AlanBarber.ResultR-VSCodeToolkit)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

ISC License - see the [LICENSE](https://github.com/AlanBarber/ResultR/blob/main/LICENSE) file for details.

---

Built with ❤️ for the C# / DotNet community.
