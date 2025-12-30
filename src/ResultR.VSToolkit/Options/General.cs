using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using ResultR.VSToolkit.Services;

namespace ResultR.VSToolkit.Options
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class GeneralOptions : UIElementDialogPage
        {
            private KeyBindingOptionsControl _control;

            protected override UIElement Child => _control ??= new KeyBindingOptionsControl();

            public override void LoadSettingsFromStorage()
            {
                base.LoadSettingsFromStorage();
                var options = General.Instance;
                if (_control != null)
                {
                    _control.LoadSettings(options);
                }
            }

            public override void SaveSettingsToStorage()
            {
                if (_control != null)
                {
                    _control.SaveSettings(General.Instance);
                }
                base.SaveSettingsToStorage();
                General.Instance.Save();
            }

            protected override void OnActivate(CancelEventArgs e)
            {
                base.OnActivate(e);
                LoadSettingsFromStorage();
            }
        }
    }

    public class General : BaseOptionModel<General>
    {
        private const string DefaultShortcut = "Ctrl + R, Ctrl + H";

        [Category("Go to Handler")]
        [DisplayName("Keyboard Shortcut")]
        [Description("The keyboard shortcut for the 'Go to Handler' command.")]
        [DefaultValue(DefaultShortcut)]
        public string KeyboardShortcut { get; set; } = DefaultShortcut;

        /// <summary>
        /// Called when options are saved. Applies the new keybinding.
        /// </summary>
        public override void Save()
        {
            base.Save();

            // Apply the keybinding after saving
            _ = KeyBindingService.ApplyKeyBindingAsync();
        }
    }
}
