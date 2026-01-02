import * as vscode from 'vscode';
import { CSharpType, SourceLocation, getConfig } from '../types';

/**
 * Finds the handler implementation for a given request type
 */
export async function findHandlerForRequest(requestType: CSharpType): Promise<SourceLocation | null> {
    const config = getConfig();
    
    const files = await vscode.workspace.findFiles(
        '**/*.cs',
        `{${config.search.excludePatterns.join(',')}}`
    );
    
    for (const fileUri of files) {
        try {
            const document = await vscode.workspace.openTextDocument(fileUri);
            const text = document.getText();
            
            const handlerLocation = findHandlerInText(text, requestType.typeName, document);
            if (handlerLocation) {
                return handlerLocation;
            }
        } catch {
            continue;
        }
    }
    
    return null;
}

/**
 * Finds a handler implementation in the given text that handles the specified request type
 */
function findHandlerInText(
    text: string,
    requestTypeName: string,
    document: vscode.TextDocument
): SourceLocation | null {
    // Pattern to match type declarations with inheritance
    // We need to be more precise - match up to the opening brace or end of line
    // Capture groups: 1=keyword, 2=class name, 3=base list (everything after : until { or newline with {)
    const handlerPattern = /\b(class|record|struct)\s+(\w+)(?:<[^>]+>)?\s*(?:\([^)]*\))?\s*:\s*([^{\n]+)/g;
    
    let match;
    while ((match = handlerPattern.exec(text)) !== null) {
        const baseList = match[3].trim();
        const handlerClassName = match[2];
        
        // Check if this specific class implements IRequestHandler for our request type
        const handlerInterfacePattern = new RegExp(
            `IRequestHandler<\\s*${escapeRegExp(requestTypeName)}\\s*(?:,|>)`
        );
        
        if (handlerInterfacePattern.test(baseList)) {
            // Calculate the position of the class name identifier
            // We need to find where the class name appears after the keyword
            const fullMatch = match[0];
            const keyword = match[1];
            
            // Find the class name position within the full match
            // It comes after the keyword and whitespace
            const afterKeyword = fullMatch.substring(keyword.length);
            const classNameOffset = keyword.length + (afterKeyword.length - afterKeyword.trimStart().length);
            
            const classNameStartIndex = match.index + classNameOffset;
            const classNameEndIndex = classNameStartIndex + handlerClassName.length;
            
            const startPosition = document.positionAt(classNameStartIndex);
            const endPosition = document.positionAt(classNameEndIndex);
            
            return {
                fileUri: document.uri,
                position: startPosition,
                range: new vscode.Range(startPosition, endPosition)
            };
        }
    }
    
    return null;
}

/**
 * Escapes special regex characters in a string
 */
function escapeRegExp(string: string): string {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
