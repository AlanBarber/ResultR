import * as vscode from 'vscode';
import { SourceLocation } from '../types';

/**
 * Navigates to the specified source location
 */
export async function navigateToLocation(location: SourceLocation): Promise<void> {
    const document = await vscode.workspace.openTextDocument(location.fileUri);
    const editor = await vscode.window.showTextDocument(document);
    
    // Move cursor to the location
    editor.selection = new vscode.Selection(location.position, location.position);
    
    // Reveal the location in the editor
    editor.revealRange(location.range, vscode.TextEditorRevealType.InCenter);
}
