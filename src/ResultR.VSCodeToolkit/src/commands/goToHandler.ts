import * as vscode from 'vscode';
import { findRequestTypeAtCursor } from '../services/csharpParser';
import { findHandlerForRequest } from '../services/handlerLocator';
import { navigateToLocation } from '../services/navigation';

export async function goToHandler(): Promise<void> {
    const editor = vscode.window.activeTextEditor;
    if (!editor) {
        vscode.window.showWarningMessage('No active editor found.');
        return;
    }

    if (editor.document.languageId !== 'csharp') {
        vscode.window.showWarningMessage('This command only works in C# files.');
        return;
    }

    const document = editor.document;
    const position = editor.selection.active;

    try {
        const requestType = await findRequestTypeAtCursor(document, position);
        if (!requestType) {
            vscode.window.showWarningMessage(
                'The symbol under the cursor is not a ResultR request type (IRequest or IRequest<T>).'
            );
            return;
        }

        const handlerLocation = await findHandlerForRequest(requestType);
        if (!handlerLocation) {
            vscode.window.showWarningMessage(
                `No handler found for '${requestType.typeName}'.`
            );
            return;
        }

        await navigateToLocation(handlerLocation);
    } catch (error) {
        const message = error instanceof Error ? error.message : 'Unknown error';
        vscode.window.showErrorMessage(`An error occurred: ${message}`);
    }
}
