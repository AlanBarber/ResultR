using System;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using ResultR.VSToolkit.Options;

namespace ResultR.VSToolkit.Services
{
    /// <summary>
    /// Service for managing keyboard bindings for the extension commands.
    /// </summary>
    internal static class KeyBindingService
    {
        private const string GoToHandlerCommandName = "Edit.GoToHandler";

        /// <summary>
        /// Applies the keyboard binding from the current options to the Go to Handler command.
        /// </summary>
        public static async Task ApplyKeyBindingAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = await VS.GetServiceAsync<DTE, DTE2>();
                if (dte == null)
                    return;

                var options = await General.GetLiveInstanceAsync();
                var shortcutString = BuildShortcutString(options.KeyboardShortcut);

                if (!string.IsNullOrEmpty(shortcutString))
                {
                    ApplyBinding(dte, GoToHandlerCommandName, shortcutString);
                }
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
            }
        }

        /// <summary>
        /// Builds the shortcut string in VS format from the saved shortcut.
        /// Converts from display format "Ctrl + R, Ctrl + H" to VS format "Global::Ctrl+R, Ctrl+H"
        /// </summary>
        private static string BuildShortcutString(string shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut) || shortcut == "(none)")
                return string.Empty;

            // Normalize the shortcut: remove extra spaces around +
            var normalized = shortcut
                .Replace(" + ", "+")
                .Replace("+ ", "+")
                .Replace(" +", "+");

            // VS format requires "Global::" prefix for global shortcuts
            return $"Global::{normalized}";
        }

        /// <summary>
        /// Applies the binding to the specified command.
        /// </summary>
        private static void ApplyBinding(DTE2 dte, string commandName, string shortcut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var command = dte.Commands.Item(commandName);
                if (command != null)
                {
                    // Set the new binding (this replaces existing bindings)
                    command.Bindings = new object[] { shortcut };
                }
            }
            catch (ArgumentException)
            {
                // Command not found - this is expected if the command hasn't been registered yet
            }
        }

        /// <summary>
        /// Gets the current keyboard binding for the Go to Handler command.
        /// </summary>
        public static async Task<string> GetCurrentBindingAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = await VS.GetServiceAsync<DTE, DTE2>();
                if (dte == null)
                    return string.Empty;

                var command = dte.Commands.Item(GoToHandlerCommandName);
                if (command?.Bindings is object[] bindings && bindings.Length > 0)
                {
                    return bindings[0]?.ToString() ?? string.Empty;
                }
            }
            catch
            {
                // Ignore errors when getting binding
            }

            return string.Empty;
        }
    }
}
