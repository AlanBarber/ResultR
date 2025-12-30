using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using ResultR.VSToolkit.Dialogs;
using ResultR.VSToolkit.Services;

namespace ResultR.VSToolkit.Commands
{
    [Command(PackageIds.AddRequestHandlerCommand)]
    internal sealed class AddRequestHandlerCommand : BaseCommand<AddRequestHandlerCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await CreateRequestHandlerAsync();
        }

        private async Task CreateRequestHandlerAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                // Get the target folder path
                var folderPath = await GetTargetFolderPathAsync();

                if (string.IsNullOrEmpty(folderPath))
                {
                    await VS.MessageBox.ShowAsync(
                        "ResultR",
                        "Could not determine the target folder. Please select a project or folder in Solution Explorer.",
                        Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_WARNING,
                        Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
                    return;
                }

                // Show the dialog
                var dialog = new CreateRequestHandlerDialog();

                // Get the VS main window as owner
                var hwnd = new System.Windows.Interop.WindowInteropHelper(dialog);
                hwnd.Owner = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

                var result = dialog.ShowDialog();

                if (result != true || string.IsNullOrEmpty(dialog.RequestName))
                    return;

                // Create the file
                var success = await RequestHandlerGeneratorService.CreateAndOpenRequestHandlerAsync(
                    dialog.RequestName,
                    folderPath);

                if (success)
                {
                    // Refresh Solution Explorer to show the new file
                    await RefreshSolutionExplorerAsync();
                }
            }
            catch (Exception ex)
            {
                await VS.MessageBox.ShowAsync(
                    "ResultR",
                    $"An error occurred: {ex.Message}",
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_CRITICAL,
                    Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK);
            }
        }

        private async Task<string> GetTargetFolderPathAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var dte = await VS.GetServiceAsync<DTE, DTE2>();
                if (dte == null)
                    return null;

                // Get selected items in Solution Explorer
                var selectedItems = dte.SelectedItems;
                if (selectedItems == null || selectedItems.Count == 0)
                    return null;

                var selectedItem = selectedItems.Item(1);

                // Check if it's a project
                if (selectedItem.Project != null)
                {
                    var projectPath = selectedItem.Project.FullName;
                    return Path.GetDirectoryName(projectPath);
                }

                // Check if it's a project item (folder or file)
                if (selectedItem.ProjectItem != null)
                {
                    var projectItem = selectedItem.ProjectItem;

                    // If it's a folder, use its path
                    if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFolder)
                    {
                        return projectItem.FileNames[1];
                    }

                    // If it's a file, use its containing folder
                    if (projectItem.Kind == EnvDTE.Constants.vsProjectItemKindPhysicalFile)
                    {
                        var filePath = projectItem.FileNames[1];
                        return Path.GetDirectoryName(filePath);
                    }

                    // For virtual folders, try to get the path
                    if (projectItem.Properties != null)
                    {
                        try
                        {
                            var fullPath = projectItem.Properties.Item("FullPath")?.Value?.ToString();
                            if (!string.IsNullOrEmpty(fullPath))
                            {
                                if (Directory.Exists(fullPath))
                                    return fullPath;
                                return Path.GetDirectoryName(fullPath);
                            }
                        }
                        catch
                        {
                            // Property doesn't exist
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
                return null;
            }
        }

        private async Task RefreshSolutionExplorerAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var dte = await VS.GetServiceAsync<DTE, DTE2>();
                if (dte != null)
                {
                    // Execute the refresh command
                    dte.ExecuteCommand("View.Refresh");
                }
            }
            catch
            {
                // Ignore refresh errors
            }
        }
    }
}
