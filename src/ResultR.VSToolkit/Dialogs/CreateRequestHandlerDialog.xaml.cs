using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ResultR.VSToolkit.Dialogs
{
    public partial class CreateRequestHandlerDialog : Window
    {
        // Regex for valid C# identifier: starts with letter or underscore, followed by letters, digits, or underscores
        private static readonly Regex ValidIdentifierRegex = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);

        public string RequestName { get; private set; }

        public CreateRequestHandlerDialog()
        {
            InitializeComponent();
            RequestNameTextBox.Focus();
        }

        private void RequestNameTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Block spaces
            if (e.Text.Contains(" "))
            {
                e.Handled = true;
                return;
            }

            // Only allow valid identifier characters
            var newText = RequestNameTextBox.Text.Insert(RequestNameTextBox.CaretIndex, e.Text);
            if (!string.IsNullOrEmpty(newText) && !IsValidPartialIdentifier(newText))
            {
                e.Handled = true;
            }
        }

        private void RequestNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var text = RequestNameTextBox.Text.Trim();

            if (string.IsNullOrEmpty(text))
            {
                OkButton.IsEnabled = false;
                ValidationMessage.Visibility = Visibility.Collapsed;
                PreviewText.Text = "This will create: {Name}Request and {Name}Handler";
                return;
            }

            if (!IsValidIdentifier(text))
            {
                OkButton.IsEnabled = false;
                ValidationMessage.Text = "Invalid name. Must be a valid C# class name (letters, digits, underscores; cannot start with a digit).";
                ValidationMessage.Visibility = Visibility.Visible;
                PreviewText.Text = "This will create: {Name}Request and {Name}Handler";
                return;
            }

            // Check if it already ends with "Request" or "Handler" - we'll strip it
            var baseName = text;
            if (baseName.EndsWith("Request"))
            {
                baseName = baseName.Substring(0, baseName.Length - 7);
            }
            else if (baseName.EndsWith("Handler"))
            {
                baseName = baseName.Substring(0, baseName.Length - 7);
            }

            if (string.IsNullOrEmpty(baseName))
            {
                OkButton.IsEnabled = false;
                ValidationMessage.Text = "Please enter a meaningful name.";
                ValidationMessage.Visibility = Visibility.Visible;
                return;
            }

            OkButton.IsEnabled = true;
            ValidationMessage.Visibility = Visibility.Collapsed;
            PreviewText.Text = $"This will create: {baseName}Request and {baseName}Handler";
        }

        private bool IsValidIdentifier(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return ValidIdentifierRegex.IsMatch(text);
        }

        private bool IsValidPartialIdentifier(string text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            // For partial input, we just check that each character is valid
            // First character must be letter or underscore
            if (!char.IsLetter(text[0]) && text[0] != '_')
                return false;

            // Remaining characters can be letters, digits, or underscores
            for (int i = 1; i < text.Length; i++)
            {
                if (!char.IsLetterOrDigit(text[i]) && text[i] != '_')
                    return false;
            }

            return true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var text = RequestNameTextBox.Text.Trim();

            // Strip "Request" or "Handler" suffix if present
            if (text.EndsWith("Request"))
            {
                text = text.Substring(0, text.Length - 7);
            }
            else if (text.EndsWith("Handler"))
            {
                text = text.Substring(0, text.Length - 7);
            }

            RequestName = text;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
