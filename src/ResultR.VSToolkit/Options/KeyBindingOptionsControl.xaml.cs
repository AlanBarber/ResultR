using System.Windows.Controls;
using Community.VisualStudio.Toolkit;

namespace ResultR.VSToolkit.Options
{
    public partial class KeyBindingOptionsControl : UserControl
    {
        public KeyBindingOptionsControl()
        {
            InitializeComponent();
        }

        public void LoadSettings(General options)
        {
            if (options != null && KeyBindingEditorControl != null)
            {
                KeyBindingEditorControl.Shortcut = options.KeyboardShortcut;
            }
        }

        public void SaveSettings(General options)
        {
            if (options != null && KeyBindingEditorControl != null)
            {
                options.KeyboardShortcut = KeyBindingEditorControl.Shortcut;
            }
        }
    }
}
