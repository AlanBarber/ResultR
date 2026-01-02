import * as vscode from 'vscode';
import { CSharpType, getConfig } from '../types';

// Interface names for ResultR types
const REQUEST_INTERFACES = ['IRequest', 'IRequest<'];
const HANDLER_INTERFACES = ['IRequestHandler<'];

interface TypeDeclaration {
    typeName: string;
    baseList: string;
    startIndex: number;
    endIndex: number;
}

/**
 * Finds the request type name at the cursor position.
 * Works when cursor is on:
 * - A type declaration that implements IRequest/IRequest<T>
 * - A variable declaration like "var myObj = new MyRequest();"
 * - A type name usage like "MyRequest request"
 */
export async function findRequestTypeAtCursor(
    document: vscode.TextDocument,
    position: vscode.Position
): Promise<CSharpType | null> {
    const text = document.getText();
    
    // Get the word at the cursor position
    const wordRange = document.getWordRangeAtPosition(position);
    if (!wordRange) {
        return null;
    }
    
    const word = document.getText(wordRange);
    
    // First, check if the word itself is a request type declaration in this file
    const typeDeclarations = findTypeDeclarations(text);
    for (const decl of typeDeclarations) {
        if (decl.typeName === word && implementsInterface(decl.baseList, REQUEST_INTERFACES)) {
            const declPosition = document.positionAt(decl.startIndex);
            const endPosition = document.positionAt(decl.endIndex);
            return {
                typeName: decl.typeName,
                fileUri: document.uri,
                position: declPosition,
                range: new vscode.Range(declPosition, endPosition)
            };
        }
    }
    
    // Check if the word is a type name used in the code (e.g., "new MyRequest()" or "MyRequest request")
    // We need to search the workspace for the type definition
    const typeInfo = await findRequestTypeInWorkspace(word);
    if (typeInfo) {
        return typeInfo;
    }
    
    // Check if cursor is on a variable that is of a request type
    // Look for patterns like "var myObj = new SomeRequest()" where cursor is on myObj
    const line = document.lineAt(position.line).text;
    const variableTypeMatch = findVariableType(line, word);
    if (variableTypeMatch) {
        const typeInfoFromVar = await findRequestTypeInWorkspace(variableTypeMatch);
        if (typeInfoFromVar) {
            return typeInfoFromVar;
        }
    }
    
    return null;
}

/**
 * Finds the handler type at the cursor position
 */
export async function findHandlerTypeAtCursor(
    document: vscode.TextDocument,
    position: vscode.Position
): Promise<CSharpType | null> {
    const text = document.getText();
    
    const wordRange = document.getWordRangeAtPosition(position);
    if (!wordRange) {
        return null;
    }
    
    const word = document.getText(wordRange);
    const typeDeclarations = findTypeDeclarations(text);
    
    for (const decl of typeDeclarations) {
        if (decl.typeName === word && implementsInterface(decl.baseList, HANDLER_INTERFACES)) {
            const declPosition = document.positionAt(decl.startIndex);
            const endPosition = document.positionAt(decl.endIndex);
            return {
                typeName: decl.typeName,
                fileUri: document.uri,
                position: declPosition,
                range: new vscode.Range(declPosition, endPosition)
            };
        }
    }
    
    return null;
}

/**
 * Finds all type declarations in the given text
 */
function findTypeDeclarations(text: string): TypeDeclaration[] {
    const declarations: TypeDeclaration[] = [];
    const regex = /\b(class|record|struct)\s+(\w+)(?:<[^>]+>)?\s*(?:\([^)]*\))?\s*(?::\s*([^{]+))?/g;
    
    let match;
    while ((match = regex.exec(text)) !== null) {
        declarations.push({
            typeName: match[2],
            baseList: match[3] || '',
            startIndex: match.index,
            endIndex: match.index + match[0].length
        });
    }
    
    return declarations;
}

/**
 * Checks if the base list contains any of the specified interface names
 */
function implementsInterface(baseList: string, interfaceNames: string[]): boolean {
    if (!baseList) {
        return false;
    }
    
    for (const interfaceName of interfaceNames) {
        if (interfaceName.endsWith('<')) {
            if (baseList.includes(interfaceName)) {
                return true;
            }
        } else {
            const pattern = new RegExp(`\\b${interfaceName}(?:<[^>]+>)?\\b`);
            if (pattern.test(baseList)) {
                return true;
            }
        }
    }
    
    return false;
}

/**
 * Tries to find the type of a variable from the line context
 * Handles patterns like:
 * - var myObj = new MyRequest();
 * - MyRequest myObj = new MyRequest();
 * - var myObj = new MyRequest { ... };
 */
function findVariableType(line: string, variableName: string): string | null {
    // Pattern: var variableName = new TypeName(...)
    const varNewPattern = new RegExp(`var\\s+${escapeRegExp(variableName)}\\s*=\\s*new\\s+(\\w+)`);
    let match = line.match(varNewPattern);
    if (match) {
        return match[1];
    }
    
    // Pattern: TypeName variableName = ...
    const typedVarPattern = new RegExp(`(\\w+)\\s+${escapeRegExp(variableName)}\\s*=`);
    match = line.match(typedVarPattern);
    if (match && match[1] !== 'var') {
        return match[1];
    }
    
    return null;
}

/**
 * Extracts the request type name from a handler's base list
 */
export function extractRequestTypeFromHandler(baseList: string): string | null {
    const match = baseList.match(/IRequestHandler<\s*(\w+)/);
    return match ? match[1] : null;
}

/**
 * Searches the workspace for a request type definition
 */
async function findRequestTypeInWorkspace(typeName: string): Promise<CSharpType | null> {
    const config = getConfig();
    
    const files = await vscode.workspace.findFiles(
        '**/*.cs',
        `{${config.search.excludePatterns.join(',')}}`
    );
    
    for (const fileUri of files) {
        try {
            const document = await vscode.workspace.openTextDocument(fileUri);
            const text = document.getText();
            const typeDeclarations = findTypeDeclarations(text);
            
            for (const decl of typeDeclarations) {
                if (decl.typeName === typeName && implementsInterface(decl.baseList, REQUEST_INTERFACES)) {
                    const declPosition = document.positionAt(decl.startIndex);
                    const endPosition = document.positionAt(decl.endIndex);
                    return {
                        typeName: decl.typeName,
                        fileUri: document.uri,
                        position: declPosition,
                        range: new vscode.Range(declPosition, endPosition)
                    };
                }
            }
        } catch {
            continue;
        }
    }
    
    return null;
}

/**
 * Escapes special regex characters
 */
function escapeRegExp(string: string): string {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
