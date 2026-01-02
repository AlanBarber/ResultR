import * as vscode from 'vscode';

/**
 * Represents a C# type found in the source code
 */
export interface CSharpType {
    /** The simple name of the type (e.g., "CreateUserRequest") */
    typeName: string;
    /** The full namespace-qualified name if available */
    fullName?: string;
    /** The file URI where this type is defined */
    fileUri: vscode.Uri;
    /** The position of the type declaration */
    position: vscode.Position;
    /** The line range of the type declaration */
    range: vscode.Range;
}

/**
 * Represents a location in a source file
 */
export interface SourceLocation {
    /** The file URI */
    fileUri: vscode.Uri;
    /** The position to navigate to */
    position: vscode.Position;
    /** The range of the symbol */
    range: vscode.Range;
}

/**
 * Configuration for the ResultR extension
 */
export interface ResultRConfig {
    goToHandler: {
        keyboardShortcut: string;
    };
    codeGeneration: {
        useFileScopedNamespaces: 'auto' | 'always' | 'never';
    };
    search: {
        excludePatterns: string[];
    };
}

/**
 * Gets the current extension configuration
 */
export function getConfig(): ResultRConfig {
    const config = vscode.workspace.getConfiguration('resultr');
    return {
        goToHandler: {
            keyboardShortcut: config.get<string>('goToHandler.keyboardShortcut', 'ctrl+r ctrl+h')
        },
        codeGeneration: {
            useFileScopedNamespaces: config.get<'auto' | 'always' | 'never'>('codeGeneration.useFileScopedNamespaces', 'auto')
        },
        search: {
            excludePatterns: config.get<string[]>('search.excludePatterns', ['**/bin/**', '**/obj/**', '**/node_modules/**'])
        }
    };
}
