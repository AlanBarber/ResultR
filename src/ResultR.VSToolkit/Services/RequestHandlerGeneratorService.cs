using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using DteProject = EnvDTE.Project;
using DteSolution = EnvDTE.Solution;

namespace ResultR.VSToolkit.Services
{
    /// <summary>
    /// Service for generating ResultR request and handler files.
    /// </summary>
    internal static class RequestHandlerGeneratorService
    {
        // Template for file-scoped namespace (C# 10+)
        private const string FileScopedTemplate = @"using ResultR;

namespace {1};

public record {0}Request() : IRequest;

public class {0}Handler : IRequestHandler<{0}Request>
{{
    public async ValueTask<Result> HandleAsync({0}Request request, CancellationToken cancellationToken)
    {{
        throw new NotImplementedException();
    }}
}}
";

        // Template for block-scoped namespace (traditional)
        private const string BlockScopedTemplate = @"using ResultR;

namespace {1}
{{
    public record {0}Request() : IRequest;

    public class {0}Handler : IRequestHandler<{0}Request>
    {{
        public async ValueTask<Result> HandleAsync({0}Request request, CancellationToken cancellationToken)
        {{
            throw new NotImplementedException();
        }}
    }}
}}
";

        /// <summary>
        /// Creates a new request/handler file with the specified name in the given folder.
        /// </summary>
        /// <param name="requestName">The base name for the request (without "Request" suffix).</param>
        /// <param name="folderPath">The folder path where the file should be created.</param>
        /// <returns>The path to the created file, or null if creation failed.</returns>
        public static async Task<string> CreateRequestHandlerFileAsync(string requestName, string folderPath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var fileName = $"{requestName}.cs";
                var filePath = Path.Combine(folderPath, fileName);

                // Check if file already exists
                if (File.Exists(filePath))
                {
                    await VS.MessageBox.ShowAsync(
                        "File Already Exists",
                        $"A file named '{fileName}' already exists in this location.",
                        Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_WARNING,
                        Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    return null;
                }

                // Detect namespace style from existing files in the project
                var useFileScopedNamespace = await DetectNamespaceStyleAsync(folderPath);

                // Calculate the namespace for this file
                var namespaceName = await CalculateNamespaceAsync(folderPath);

                // Generate file content with appropriate template
                var template = useFileScopedNamespace ? FileScopedTemplate : BlockScopedTemplate;
                var content = string.Format(template, requestName, namespaceName);

                // Write the file
                File.WriteAllText(filePath, content);

                return filePath;
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowAsync(
                    "Error Creating File",
                    $"An error occurred while creating the file: {ex.Message}",
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_CRITICAL,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
                return null;
            }
        }

        /// <summary>
        /// Detects whether the project uses file-scoped namespaces by examining existing .cs files.
        /// </summary>
        private static async Task<bool> DetectNamespaceStyleAsync(string folderPath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                // Look for .cs files in the folder and parent folders
                var directoryToSearch = folderPath;

                // Search up to 7 levels up to find a representative file
                for (int i = 0; i < 7 && !string.IsNullOrEmpty(directoryToSearch); i++)
                {
                    var csFiles = Directory.GetFiles(directoryToSearch, "*.cs", SearchOption.TopDirectoryOnly);

                    foreach (var file in csFiles)
                    {
                        // Skip designer files and auto-generated files
                        var fileName = Path.GetFileName(file);
                        if (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
                            fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                            fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var content = File.ReadAllText(file);

                        // Check for file-scoped namespace (ends with semicolon, not followed by opening brace)
                        // Pattern: namespace Something.Something;
                        if (Regex.IsMatch(content, @"^\s*namespace\s+[\w.]+\s*;\s*$", RegexOptions.Multiline))
                        {
                            return true;
                        }

                        // Check for block-scoped namespace (followed by opening brace)
                        // Pattern: namespace Something.Something {
                        if (Regex.IsMatch(content, @"^\s*namespace\s+[\w.]+\s*\{", RegexOptions.Multiline))
                        {
                            return false;
                        }
                    }

                    directoryToSearch = Path.GetDirectoryName(directoryToSearch);
                }

                // Default to file-scoped for modern projects
                return true;
            }
            catch
            {
                // Default to file-scoped on error
                return true;
            }
        }

        /// <summary>
        /// Calculates the namespace for a file based on the project's root namespace and folder structure.
        /// </summary>
        private static async Task<string> CalculateNamespaceAsync(string folderPath)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = await VS.GetServiceAsync<DTE, DTE2>();
                if (dte == null)
                    return "MyNamespace";

                // Try to find the project that contains this folder
                var project = FindProjectForFolder(dte.Solution, folderPath);

                if (project == null)
                    return "MyNamespace";

                // Get the project's root namespace
                var rootNamespace = GetProjectRootNamespace(project);

                if (string.IsNullOrEmpty(rootNamespace))
                    rootNamespace = project.Name;

                // Get the project directory
                var projectPath = project.FullName;
                var projectDir = Path.GetDirectoryName(projectPath);

                if (string.IsNullOrEmpty(projectDir))
                    return rootNamespace;

                // Calculate relative path from project to folder
                if (folderPath.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = folderPath.Substring(projectDir.Length)
                        .Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (!string.IsNullOrEmpty(relativePath))
                    {
                        // Convert path separators to namespace separators
                        var namespaceSuffix = relativePath
                            .Replace(Path.DirectorySeparatorChar, '.')
                            .Replace(Path.AltDirectorySeparatorChar, '.');

                        return $"{rootNamespace}.{namespaceSuffix}";
                    }
                }

                return rootNamespace;
            }
            catch
            {
                return "MyNamespace";
            }
        }

        /// <summary>
        /// Finds the project that contains the specified folder.
        /// </summary>
        private static DteProject FindProjectForFolder(DteSolution solution, string folderPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                foreach (DteProject project in solution.Projects)
                {
                    var found = FindProjectRecursive(project, folderPath);
                    if (found != null)
                        return found;
                }
            }
            catch
            {
                // Ignore errors
            }

            return null;
        }

        private static DteProject FindProjectRecursive(DteProject project, string folderPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Check if this is a solution folder
                if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
                {
                    // Search nested projects
                    foreach (ProjectItem item in project.ProjectItems)
                    {
                        if (item.SubProject != null)
                        {
                            var found = FindProjectRecursive(item.SubProject, folderPath);
                            if (found != null)
                                return found;
                        }
                    }
                    return null;
                }

                // Check if the folder is within this project's directory
                var projectPath = project.FullName;
                if (!string.IsNullOrEmpty(projectPath))
                {
                    var projectDir = Path.GetDirectoryName(projectPath);
                    if (!string.IsNullOrEmpty(projectDir) &&
                        folderPath.StartsWith(projectDir, StringComparison.OrdinalIgnoreCase))
                    {
                        return project;
                    }
                }
            }
            catch
            {
                // Ignore errors for inaccessible projects
            }

            return null;
        }

        /// <summary>
        /// Gets the root namespace from project properties.
        /// </summary>
        private static string GetProjectRootNamespace(DteProject project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                // Try to get RootNamespace property
                var rootNamespaceProp = project.Properties?.Item("RootNamespace");
                if (rootNamespaceProp?.Value != null)
                {
                    return rootNamespaceProp.Value.ToString();
                }

                // Try DefaultNamespace as fallback
                var defaultNamespaceProp = project.Properties?.Item("DefaultNamespace");
                if (defaultNamespaceProp?.Value != null)
                {
                    return defaultNamespaceProp.Value.ToString();
                }
            }
            catch
            {
                // Property doesn't exist
            }

            return project.Name;
        }

        /// <summary>
        /// Creates the file and adds it to the project, then opens it in the editor.
        /// </summary>
        public static async Task<bool> CreateAndOpenRequestHandlerAsync(string requestName, string folderPath)
        {
            var filePath = await CreateRequestHandlerFileAsync(requestName, folderPath);

            if (string.IsNullOrEmpty(filePath))
                return false;

            try
            {
                // Open the file in the editor
                await VS.Documents.OpenAsync(filePath);

                return true;
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                return false;
            }
        }
    }
}
