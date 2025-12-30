global using Task = System.Threading.Tasks.Task;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using ResultR.VSToolkit.Options;
using ResultR.VSToolkit.Services;

namespace ResultR.VSToolkit
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "ResultR", "General", 0, 0, true, SupportsProfiles = true)]
    [Guid(PackageGuids.ResultRVSToolkitString)]
    public sealed class ResultRVSToolkitPackage : ToolkitPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();

            this.RegisterToolWindows();

            // Apply saved keybinding settings on startup
            await ApplySavedKeyBindingAsync();
        }

        private async Task ApplySavedKeyBindingAsync()
        {
            try
            {
                // Wait a bit for VS to fully initialize before applying keybindings
                await Task.Delay(1000);
                await KeyBindingService.ApplyKeyBindingAsync();
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
            }
        }
    }
}