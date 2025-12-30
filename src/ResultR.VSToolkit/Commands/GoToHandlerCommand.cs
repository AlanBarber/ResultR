using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using RoslynSolution = Microsoft.CodeAnalysis.Solution;

namespace ResultR.VSToolkit
{
    [Command(PackageIds.GoToHandlerCommand)]
    internal sealed class GoToHandlerCommand : BaseCommand<GoToHandlerCommand>
    {
        private const string IRequestInterfaceName = "ResultR.IRequest";
        private const string IRequestGenericInterfaceName = "ResultR.IRequest`1";
        private const string IRequestHandlerInterfaceName = "ResultR.IRequestHandler`1";
        private const string IRequestHandlerGenericInterfaceName = "ResultR.IRequestHandler`2";

        protected override void BeforeQueryStatus(EventArgs e)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                var isOnRequest = await IsOnRequestTypeAsync();
                Command.Visible = isOnRequest;
                Command.Enabled = true;
            });
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await GoToHandlerAsync();
        }

        private async Task<bool> IsOnRequestTypeAsync()
        {
            try
            {
                var (workspace, document, position) = await GetCurrentDocumentInfoAsync();
                if (workspace == null || document == null)
                    return false;

                var requestTypeSymbol = await GetRequestTypeAtPositionAsync(document, position);
                return requestTypeSymbol != null;
            }
            catch
            {
                return false;
            }
        }

        private async Task GoToHandlerAsync()
        {
            try
            {
                var (workspace, document, position) = await GetCurrentDocumentInfoAsync();
                if (workspace == null || document == null)
                {
                    await VS.MessageBox.ShowAsync("Go to Handler", "No active document found.", Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_WARNING, Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    return;
                }

                var requestTypeSymbol = await GetRequestTypeAtPositionAsync(document, position);
                if (requestTypeSymbol == null)
                {
                    await VS.MessageBox.ShowAsync("Go to Handler", "The symbol under the cursor is not a ResultR request type.", Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_WARNING, Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    return;
                }

                var handlerLocation = await FindHandlerLocationAsync(workspace.CurrentSolution, requestTypeSymbol);
                if (handlerLocation == null)
                {
                    await VS.MessageBox.ShowAsync("Go to Handler", $"No handler found for '{requestTypeSymbol.Name}'.", Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_WARNING, Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    return;
                }

                await NavigateToLocationAsync(handlerLocation);
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowAsync("Go to Handler", $"An error occurred: {ex.Message}", Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_CRITICAL, Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
            }
        }

        private async Task<(Workspace, Document, int)> GetCurrentDocumentInfoAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView == null)
                return (null, null, 0);

            var textBuffer = docView.TextView.TextBuffer;
            var caretPosition = docView.TextView.Caret.Position.BufferPosition.Position;

            var workspace = textBuffer.GetWorkspace();
            if (workspace == null)
                return (null, null, 0);

            var documentId = workspace.GetDocumentIdInCurrentContext(textBuffer.AsTextContainer());
            if (documentId == null)
                return (null, null, 0);

            var document = workspace.CurrentSolution.GetDocument(documentId);
            return (workspace, document, caretPosition);
        }

        private async Task<INamedTypeSymbol> GetRequestTypeAtPositionAsync(Document document, int position)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var syntaxRoot = await document.GetSyntaxRootAsync();

            if (semanticModel == null || syntaxRoot == null)
                return null;

            var token = syntaxRoot.FindToken(position);
            if (token == default)
                return null;

            var node = token.Parent;

            // Try to get the symbol from the node
            ITypeSymbol typeSymbol = null;

            // Check if we're on a type name (e.g., CreateUserRequest in declaration or usage)
            if (node is IdentifierNameSyntax || node is GenericNameSyntax)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(node);
                var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

                if (symbol is INamedTypeSymbol namedType)
                {
                    typeSymbol = namedType;
                }
                else if (symbol is ILocalSymbol localSymbol)
                {
                    typeSymbol = localSymbol.Type;
                }
                else if (symbol is IParameterSymbol parameterSymbol)
                {
                    typeSymbol = parameterSymbol.Type;
                }
                else if (symbol is IFieldSymbol fieldSymbol)
                {
                    typeSymbol = fieldSymbol.Type;
                }
                else if (symbol is IPropertySymbol propertySymbol)
                {
                    typeSymbol = propertySymbol.Type;
                }
                else if (symbol is IMethodSymbol methodSymbol)
                {
                    typeSymbol = methodSymbol.ReturnType;
                }
            }

            // Check if we're on a variable declaration
            if (typeSymbol == null)
            {
                var variableDeclarator = node.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
                if (variableDeclarator != null)
                {
                    var declaredSymbol = semanticModel.GetDeclaredSymbol(variableDeclarator);
                    if (declaredSymbol is ILocalSymbol local)
                    {
                        typeSymbol = local.Type;
                    }
                }
            }

            // Check if we're on a type declaration (record, class, struct)
            if (typeSymbol == null)
            {
                var typeDeclaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if (typeDeclaration != null)
                {
                    var declaredSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration);
                    if (declaredSymbol is INamedTypeSymbol namedType)
                    {
                        typeSymbol = namedType;
                    }
                }
            }

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (ImplementsIRequest(namedTypeSymbol))
                {
                    return namedTypeSymbol;
                }
            }

            return null;
        }

        private bool ImplementsIRequest(INamedTypeSymbol typeSymbol)
        {
            // Check all interfaces implemented by this type
            foreach (var iface in typeSymbol.AllInterfaces)
            {
                var fullName = GetFullMetadataName(iface);
                if (fullName == IRequestInterfaceName || fullName == IRequestGenericInterfaceName)
                {
                    return true;
                }

                // Check for the original definition in case of generic interfaces
                if (iface.OriginalDefinition != null)
                {
                    var originalFullName = GetFullMetadataName(iface.OriginalDefinition);
                    if (originalFullName == IRequestInterfaceName || originalFullName == IRequestGenericInterfaceName)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private async Task<Location> FindHandlerLocationAsync(RoslynSolution solution, INamedTypeSymbol requestTypeSymbol)
        {
            // Look for IRequestHandler<TRequest> or IRequestHandler<TRequest, TResponse>
            foreach (var project in solution.Projects)
            {
                var compilation = await project.GetCompilationAsync();
                if (compilation == null)
                    continue;

                // Search all types in the compilation
                var allTypes = GetAllTypesInCompilation(compilation);

                foreach (var typeSymbol in allTypes)
                {
                    if (IsHandlerForRequest(typeSymbol, requestTypeSymbol))
                    {
                        // Found the handler, get its location
                        var location = typeSymbol.Locations.FirstOrDefault(l => l.IsInSource);
                        if (location != null)
                        {
                            return location;
                        }
                    }
                }
            }

            return null;
        }

        private IEnumerable<INamedTypeSymbol> GetAllTypesInCompilation(Compilation compilation)
        {
            var types = new List<INamedTypeSymbol>();
            GetAllTypesInNamespace(compilation.GlobalNamespace, types);
            return types;
        }

        private void GetAllTypesInNamespace(INamespaceSymbol namespaceSymbol, List<INamedTypeSymbol> types)
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                types.Add(type);
                GetNestedTypes(type, types);
            }

            foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                GetAllTypesInNamespace(nestedNamespace, types);
            }
        }

        private void GetNestedTypes(INamedTypeSymbol type, List<INamedTypeSymbol> types)
        {
            foreach (var nestedType in type.GetTypeMembers())
            {
                types.Add(nestedType);
                GetNestedTypes(nestedType, types);
            }
        }

        private bool IsHandlerForRequest(INamedTypeSymbol handlerType, INamedTypeSymbol requestType)
        {
            foreach (var iface in handlerType.AllInterfaces)
            {
                var originalDef = iface.OriginalDefinition;
                var fullName = GetFullMetadataName(originalDef);

                // Check if it's IRequestHandler<T> or IRequestHandler<T, TResponse>
                if (fullName == IRequestHandlerInterfaceName || fullName == IRequestHandlerGenericInterfaceName)
                {
                    // Check if the first type argument is our request type
                    if (iface.TypeArguments.Length > 0)
                    {
                        var firstTypeArg = iface.TypeArguments[0];
                        if (SymbolEqualityComparer.Default.Equals(firstTypeArg, requestType))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private string GetFullMetadataName(ISymbol symbol)
        {
            if (symbol == null)
                return string.Empty;

            if (symbol is INamedTypeSymbol namedType)
            {
                var containingNamespace = namedType.ContainingNamespace;
                var namespaceName = containingNamespace?.IsGlobalNamespace == true
                    ? string.Empty
                    : containingNamespace?.ToDisplayString();

                var typeName = namedType.MetadataName;

                return string.IsNullOrEmpty(namespaceName)
                    ? typeName
                    : $"{namespaceName}.{typeName}";
            }

            return symbol.ToDisplayString();
        }

        private async Task NavigateToLocationAsync(Location location)
        {
            var lineSpan = location.GetLineSpan();
            var filePath = lineSpan.Path;
            var lineNumber = lineSpan.StartLinePosition.Line;
            var columnNumber = lineSpan.StartLinePosition.Character;

            await VS.Documents.OpenAsync(filePath);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView != null)
            {
                var textView = docView.TextView;
                var snapshot = textView.TextSnapshot;

                if (lineNumber < snapshot.LineCount)
                {
                    var line = snapshot.GetLineFromLineNumber(lineNumber);
                    var targetPosition = line.Start.Position + Math.Min(columnNumber, line.Length);

                    var snapshotPoint = new SnapshotPoint(snapshot, targetPosition);
                    textView.Caret.MoveTo(snapshotPoint);
                    textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(snapshotPoint, 0));
                }
            }
        }
    }
}
