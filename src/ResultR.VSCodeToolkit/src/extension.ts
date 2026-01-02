import * as vscode from 'vscode';
import { goToHandler } from './commands/goToHandler';
import { newRequestHandler } from './commands/newRequestHandler';

export function activate(context: vscode.ExtensionContext) {
    const goToHandlerDisposable = vscode.commands.registerCommand(
        'resultr.goToHandler',
        goToHandler
    );

    const newRequestHandlerDisposable = vscode.commands.registerCommand(
        'resultr.newRequestHandler',
        newRequestHandler
    );

    context.subscriptions.push(goToHandlerDisposable, newRequestHandlerDisposable);
}

export function deactivate() {
    // Nothing to clean up
}
