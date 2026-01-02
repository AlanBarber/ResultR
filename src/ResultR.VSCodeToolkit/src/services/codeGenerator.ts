import * as vscode from 'vscode';
import * as path from 'path';
import { getConfig } from '../types';

// Template for file-scoped namespace (C# 10+)
const FILE_SCOPED_TEMPLATE = `using ResultR;

namespace {namespace};

public record {name}Request() : IRequest;

public class {name}Handler : IRequestHandler<{name}Request>
{
    public async ValueTask<Result> HandleAsync({name}Request request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
`;

// Template for block-scoped namespace (traditional)
const BLOCK_SCOPED_TEMPLATE = `using ResultR;

namespace {namespace}
{
    public record {name}Request() : IRequest;

    public class {name}Handler : IRequestHandler<{name}Request>
    {
        public async ValueTask<Result> HandleAsync({name}Request request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
`;

/**
 * Generates a new ResultR request and handler file
 */
export async function generateRequestHandler(
    requestName: string,
    targetFolder: vscode.Uri
): Promise<vscode.Uri | null> {
    const fileName = `${requestName}.cs`;
    const filePath = vscode.Uri.joinPath(targetFolder, fileName);

    // Check if file already exists
    try {
        await vscode.workspace.fs.stat(filePath);
        vscode.window.showWarningMessage(`A file named '${fileName}' already exists in this location.`);
        return null;
    } catch {
        // File doesn't exist, which is what we want
    }

    // Detect namespace style from existing files
    const useFileScopedNamespace = await detectNamespaceStyle(targetFolder);

    // Calculate the namespace
    const namespaceName = await calculateNamespace(targetFolder);

    // Generate file content
    const template = useFileScopedNamespace ? FILE_SCOPED_TEMPLATE : BLOCK_SCOPED_TEMPLATE;
    const content = template
        .replace(/{name}/g, requestName)
        .replace(/{namespace}/g, namespaceName);

    // Write the file
    const encoder = new TextEncoder();
    await vscode.workspace.fs.writeFile(filePath, encoder.encode(content));

    return filePath;
}

/**
 * Detects whether the project uses file-scoped namespaces by examining existing .cs files
 */
async function detectNamespaceStyle(folderUri: vscode.Uri): Promise<boolean> {
    const config = getConfig();

    // Check configuration first
    if (config.codeGeneration.useFileScopedNamespaces === 'always') {
        return true;
    }
    if (config.codeGeneration.useFileScopedNamespaces === 'never') {
        return false;
    }

    // Auto-detect from existing files
    try {
        // Search for .cs files in the folder and parent folders
        let currentFolder = folderUri;
        const workspaceFolder = vscode.workspace.getWorkspaceFolder(folderUri);
        const rootPath = workspaceFolder?.uri.fsPath || '';

        for (let i = 0; i < 7 && currentFolder.fsPath.length > rootPath.length; i++) {
            const files = await vscode.workspace.findFiles(
                new vscode.RelativePattern(currentFolder, '*.cs'),
                null,
                10
            );

            for (const file of files) {
                const fileName = path.basename(file.fsPath);
                // Skip designer and auto-generated files
                if (fileName.endsWith('.Designer.cs') ||
                    fileName.endsWith('.g.cs') ||
                    fileName.endsWith('.g.i.cs')) {
                    continue;
                }

                const document = await vscode.workspace.openTextDocument(file);
                const text = document.getText();

                // Check for file-scoped namespace (ends with semicolon)
                if (/^\s*namespace\s+[\w.]+\s*;\s*$/m.test(text)) {
                    return true;
                }

                // Check for block-scoped namespace (followed by opening brace)
                if (/^\s*namespace\s+[\w.]+\s*\{/m.test(text)) {
                    return false;
                }
            }

            // Move to parent folder
            currentFolder = vscode.Uri.joinPath(currentFolder, '..');
        }
    } catch {
        // Ignore errors during detection
    }

    // Default to file-scoped for modern projects
    return true;
}

/**
 * Calculates the namespace for a file based on the project structure
 */
async function calculateNamespace(folderUri: vscode.Uri): Promise<string> {
    try {
        // Find the .csproj file to determine the root namespace
        const workspaceFolder = vscode.workspace.getWorkspaceFolder(folderUri);
        if (!workspaceFolder) {
            return 'MyNamespace';
        }

        // Search for .csproj files
        const csprojFiles = await vscode.workspace.findFiles(
            new vscode.RelativePattern(workspaceFolder, '**/*.csproj'),
            '**/node_modules/**',
            10
        );

        // Find the closest .csproj to our target folder
        let closestProject: { uri: vscode.Uri; rootNamespace: string } | null = null;
        let closestDistance = Infinity;

        for (const csprojUri of csprojFiles) {
            const projectDir = vscode.Uri.joinPath(csprojUri, '..');
            
            // Check if target folder is within this project
            if (folderUri.fsPath.startsWith(projectDir.fsPath)) {
                const distance = folderUri.fsPath.length - projectDir.fsPath.length;
                if (distance < closestDistance) {
                    closestDistance = distance;
                    const rootNamespace = await extractRootNamespace(csprojUri);
                    closestProject = { uri: projectDir, rootNamespace };
                }
            }
        }

        if (closestProject) {
            // Calculate relative path from project root to target folder
            const projectPath = closestProject.uri.fsPath;
            const targetPath = folderUri.fsPath;

            if (targetPath.startsWith(projectPath)) {
                let relativePath = targetPath.substring(projectPath.length);
                // Remove leading separator
                if (relativePath.startsWith(path.sep)) {
                    relativePath = relativePath.substring(1);
                }

                if (relativePath) {
                    // Convert path separators to namespace separators
                    const namespaceSuffix = relativePath.split(path.sep).join('.');
                    return `${closestProject.rootNamespace}.${namespaceSuffix}`;
                }

                return closestProject.rootNamespace;
            }
        }

        // Fallback: use the folder name
        return path.basename(folderUri.fsPath) || 'MyNamespace';
    } catch {
        return 'MyNamespace';
    }
}

/**
 * Extracts the RootNamespace from a .csproj file
 */
async function extractRootNamespace(csprojUri: vscode.Uri): Promise<string> {
    try {
        const document = await vscode.workspace.openTextDocument(csprojUri);
        const text = document.getText();

        // Look for <RootNamespace>...</RootNamespace>
        const match = text.match(/<RootNamespace>([^<]+)<\/RootNamespace>/);
        if (match) {
            return match[1];
        }

        // Fallback to project file name (without .csproj extension)
        return path.basename(csprojUri.fsPath, '.csproj');
    } catch {
        return path.basename(csprojUri.fsPath, '.csproj');
    }
}
