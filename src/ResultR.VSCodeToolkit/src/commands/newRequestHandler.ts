import * as vscode from 'vscode';
import { generateRequestHandler } from '../services/codeGenerator';

/**
 * Command to create a new ResultR Request and Handler pair.
 * Can be invoked from the explorer context menu on a folder or file.
 */
export async function newRequestHandler(uri?: vscode.Uri): Promise<void> {
    // Determine the target folder
    let targetFolder: vscode.Uri | undefined;

    if (uri) {
        // Called from explorer context menu
        const stat = await vscode.workspace.fs.stat(uri);
        if (stat.type === vscode.FileType.Directory) {
            targetFolder = uri;
        } else {
            // It's a file, use its parent directory
            targetFolder = vscode.Uri.joinPath(uri, '..');
        }
    } else {
        // Called from command palette - ask user to select a folder
        const workspaceFolders = vscode.workspace.workspaceFolders;
        if (!workspaceFolders || workspaceFolders.length === 0) {
            vscode.window.showWarningMessage('No workspace folder open.');
            return;
        }

        const folderUri = await vscode.window.showOpenDialog({
            canSelectFiles: false,
            canSelectFolders: true,
            canSelectMany: false,
            defaultUri: workspaceFolders[0].uri,
            openLabel: 'Select Folder'
        });

        if (!folderUri || folderUri.length === 0) {
            return;
        }

        targetFolder = folderUri[0];
    }

    if (!targetFolder) {
        vscode.window.showWarningMessage('Could not determine target folder.');
        return;
    }

    // Show input box for the request name
    const requestName = await vscode.window.showInputBox({
        prompt: 'Enter the request name (e.g., "CreateUser")',
        placeHolder: 'CreateUser',
        validateInput: validateRequestName
    });

    if (!requestName) {
        return; // User cancelled
    }

    // Strip "Request" or "Handler" suffix if user included it
    let baseName = requestName.trim();
    if (baseName.endsWith('Request')) {
        baseName = baseName.slice(0, -7);
    } else if (baseName.endsWith('Handler')) {
        baseName = baseName.slice(0, -7);
    }

    if (!baseName) {
        vscode.window.showWarningMessage('Please enter a meaningful name.');
        return;
    }

    try {
        const filePath = await generateRequestHandler(baseName, targetFolder);
        if (filePath) {
            // Open the generated file
            const document = await vscode.workspace.openTextDocument(filePath);
            await vscode.window.showTextDocument(document);
            vscode.window.showInformationMessage(`Created ${baseName}.cs`);
        }
    } catch (error) {
        const message = error instanceof Error ? error.message : 'Unknown error';
        vscode.window.showErrorMessage(`Failed to create request/handler: ${message}`);
    }
}

/**
 * Validates the request name input
 */
function validateRequestName(value: string): string | undefined {
    if (!value || value.trim().length === 0) {
        return 'Name is required';
    }

    const trimmed = value.trim();

    // Must be a valid C# identifier
    if (!/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(trimmed)) {
        return 'Invalid name. Must be a valid C# identifier (letters, digits, underscores; cannot start with a digit).';
    }

    return undefined; // Valid
}
