# ResultR VS Toolkit

Supercharge your [ResultR](https://github.com/AlanBarber/ResultR) development workflow with instant navigation from requests to handlers and one-click scaffolding of new request/handler pairs. The ResultR VS Toolkit is the essential companion extension for developers using the [ResultR](https://github.com/AlanBarber/ResultR) library.

## Features

### ⚡Navigate Your Codebase Instantly 

Tired of hunting through your solution to find the handler for a request? The ResultR VS Toolkit makes navigation effortless! Simply place your cursor on any IRequest type - whether it's a variable, parameter, or class definition - and press Ctrl+R, Ctrl+H (or right-click and select "Go to Handler..."). The toolkit instantly locates and opens the corresponding IRequestHandler implementation, even if it's in a completely different project. No more manual searching, no more wasted time. Just click and you're there!

### 📝 Scaffold New Request/Handler Pairs

Creating new request/handler pairs has never been easier! Right-click on any project or folder in Solution Explorer and select "ResultR Request / Handler..." from the Add menu. Enter your request name, and the toolkit generates a properly structured .cs file with the correct namespace (automatically detecting whether you use file-scoped or block-scoped namespaces), all the necessary using statements, and a ready-to-implement handler class. The generated code follows your project's existing conventions, so it fits right in with your codebase. Spend less time on boilerplate and more time on what matters - your business logic! 

## Requirements

- Visual Studio 2022 (17.0 or later)
- Projects using the [ResultR](https://github.com/AlanBarber/ResultR) library

## Installation

1. Download the `.vsix` file from the [releases page](https://github.com/AlanBarber/ResultR/releases)
2. Double-click to install
3. Restart Visual Studio

Or search for "ResultR" in the Visual Studio Extensions marketplace.

## ❓ Why ResultR VS Toolkit?

The ResultR VS Toolkit provides a simple way to navigate between `IRequest`/`IRequest<T>` classes and their corresponding `IRequestHandler` implementations. It also provides a scaffold for creating new request/handler pairs. It's the perfect companion to the [ResultR](https://github.com/AlanBarber/ResultR) library!

## 💬 Support

- **Documentation**: [GitHub Wiki](https://github.com/AlanBarber/ResultR/wiki/ResultR.VSToolkit)
- **Issues**: [GitHub Issues](https://github.com/AlanBarber/ResultR/issues)


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
