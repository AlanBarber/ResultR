using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Community.VisualStudio.Toolkit;

namespace ResultR.VSToolkit.Options
{
    public partial class KeyBindingEditor : UserControl, INotifyPropertyChanged
    {
        private const string DefaultShortcut = "Ctrl + R, Ctrl + H";
        private const int ChordTimeoutMs = 3000;

        private string _shortcutDisplay = DefaultShortcut;
        private KeyCombination _firstCombination;
        private KeyCombination _secondCombination;
        private DateTime _firstCombinationTime;
        private bool _isCapturing;
        private bool _waitingForSecondChord;

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler ShortcutChanged;

        public KeyBindingEditor()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string ShortcutDisplay
        {
            get => _shortcutDisplay;
            private set
            {
                if (_shortcutDisplay != value)
                {
                    _shortcutDisplay = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the full shortcut string in VS format (e.g., "Ctrl+R, Ctrl+H").
        /// </summary>
        public string Shortcut
        {
            get => BuildShortcutString();
            set => ParseAndSetShortcut(value);
        }

        /// <summary>
        /// Gets whether this is a chord (two-part) shortcut.
        /// </summary>
        public bool IsChord => _secondCombination != null && _secondCombination.Key != Key.None;

        private void ShortcutTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _isCapturing = true;
            _waitingForSecondChord = false;
            // Use a subtle highlight that works with both light and dark themes
            ShortcutTextBox.Background = new SolidColorBrush(Color.FromArgb(40, 0, 122, 204));
            ShortcutDisplay = "Press shortcut keys...";
        }

        private void ShortcutTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _isCapturing = false;
            _waitingForSecondChord = false;
            ShortcutTextBox.ClearValue(BackgroundProperty);
            UpdateDisplay();
        }

        private void ShortcutTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isCapturing)
                return;

            e.Handled = true;

            // Ignore modifier-only presses
            if (IsModifierKey(e.Key))
                return;

            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            var modifiers = Keyboard.Modifiers;

            // Must have at least one modifier
            if (modifiers == ModifierKeys.None)
            {
                ShortcutDisplay = "Please include a modifier key (Ctrl, Alt, or Shift)";
                return;
            }

            var combination = new KeyCombination(modifiers, key);

            if (_waitingForSecondChord)
            {
                // Check if within timeout for chord
                if ((DateTime.Now - _firstCombinationTime).TotalMilliseconds < ChordTimeoutMs)
                {
                    _secondCombination = combination;
                    _waitingForSecondChord = false;
                    UpdateDisplay();
                    ShortcutChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // Timeout expired, start fresh
                    _firstCombination = combination;
                    _secondCombination = null;
                    _firstCombinationTime = DateTime.Now;
                    _waitingForSecondChord = true;
                    ShortcutDisplay = $"{combination}, (press second chord or wait)";
                }
            }
            else
            {
                // First combination
                _firstCombination = combination;
                _secondCombination = null;
                _firstCombinationTime = DateTime.Now;
                _waitingForSecondChord = true;
                ShortcutDisplay = $"{combination}, (press second chord or release for single)";
            }
        }

        private void ShortcutTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (!_isCapturing || !_waitingForSecondChord)
                return;

            // If all modifier keys are released, finalize as a single shortcut
            if (Keyboard.Modifiers == ModifierKeys.None)
            {
                // Use single shortcut if no second chord was pressed
                if (_secondCombination == null || _secondCombination.Key == Key.None)
                {
                    _waitingForSecondChord = false;
                    UpdateDisplay();
                    ShortcutChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _firstCombination = null;
            _secondCombination = null;
            _waitingForSecondChord = false;
            ShortcutDisplay = "(none)";
            ShortcutChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ParseAndSetShortcut(DefaultShortcut);
            UpdateDisplay();
            ShortcutChanged?.Invoke(this, EventArgs.Empty);
        }

        private bool IsModifierKey(Key key)
        {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LWin || key == Key.RWin ||
                   key == Key.System;
        }

        private void UpdateDisplay()
        {
            if (_firstCombination == null || _firstCombination.Key == Key.None)
            {
                ShortcutDisplay = "(none)";
                return;
            }

            if (_secondCombination != null && _secondCombination.Key != Key.None)
            {
                ShortcutDisplay = $"{_firstCombination}, {_secondCombination}";
            }
            else
            {
                ShortcutDisplay = _firstCombination.ToString();
            }
        }

        private string BuildShortcutString()
        {
            if (_firstCombination == null || _firstCombination.Key == Key.None)
                return string.Empty;

            if (_secondCombination != null && _secondCombination.Key != Key.None)
            {
                return $"{_firstCombination.ToVsString()}, {_secondCombination.ToVsString()}";
            }

            return _firstCombination.ToVsString();
        }

        private void ParseAndSetShortcut(string shortcut)
        {
            if (string.IsNullOrWhiteSpace(shortcut))
            {
                _firstCombination = null;
                _secondCombination = null;
                return;
            }

            var parts = shortcut.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 1)
            {
                _firstCombination = KeyCombination.Parse(parts[0].Trim());
            }

            if (parts.Length >= 2)
            {
                _secondCombination = KeyCombination.Parse(parts[1].Trim());
            }
            else
            {
                _secondCombination = null;
            }

            UpdateDisplay();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Represents a single key combination (modifier + key).
    /// </summary>
    internal class KeyCombination
    {
        public ModifierKeys Modifiers { get; }
        public Key Key { get; }

        public KeyCombination(ModifierKeys modifiers, Key key)
        {
            Modifiers = modifiers;
            Key = key;
        }

        public override string ToString()
        {
            var parts = new List<string>();

            if (Modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");

            parts.Add(GetKeyDisplayName(Key));

            return string.Join(" + ", parts);
        }

        public string ToVsString()
        {
            var parts = new List<string>();

            if (Modifiers.HasFlag(ModifierKeys.Control))
                parts.Add("Ctrl");
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                parts.Add("Alt");
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                parts.Add("Shift");

            parts.Add(GetKeyDisplayName(Key));

            return string.Join("+", parts);
        }

        public static KeyCombination Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new KeyCombination(ModifierKeys.None, Key.None);

            var modifiers = ModifierKeys.None;
            var key = Key.None;

            // Normalize separators
            text = text.Replace(" + ", "+").Replace(" ", "");
            var parts = text.Split('+');

            foreach (var part in parts)
            {
                var normalized = part.Trim().ToLowerInvariant();

                switch (normalized)
                {
                    case "ctrl":
                    case "control":
                        modifiers |= ModifierKeys.Control;
                        break;
                    case "alt":
                        modifiers |= ModifierKeys.Alt;
                        break;
                    case "shift":
                        modifiers |= ModifierKeys.Shift;
                        break;
                    default:
                        // Try to parse as a key
                        if (Enum.TryParse<Key>(part.Trim(), true, out var parsedKey))
                        {
                            key = parsedKey;
                        }
                        else if (part.Trim().Length == 1)
                        {
                            // Single character, try to map to key
                            var c = char.ToUpperInvariant(part.Trim()[0]);
                            if (c >= 'A' && c <= 'Z')
                            {
                                key = (Key)(c - 'A' + (int)Key.A);
                            }
                            else if (c >= '0' && c <= '9')
                            {
                                key = (Key)(c - '0' + (int)Key.D0);
                            }
                        }
                        break;
                }
            }

            return new KeyCombination(modifiers, key);
        }

        private static string GetKeyDisplayName(Key key)
        {
            // Handle special keys
            return key switch
            {
                Key.D0 => "0",
                Key.D1 => "1",
                Key.D2 => "2",
                Key.D3 => "3",
                Key.D4 => "4",
                Key.D5 => "5",
                Key.D6 => "6",
                Key.D7 => "7",
                Key.D8 => "8",
                Key.D9 => "9",
                Key.OemMinus => "-",
                Key.OemPlus => "=",
                Key.OemOpenBrackets => "[",
                Key.OemCloseBrackets => "]",
                Key.OemPipe => "\\",
                Key.OemSemicolon => ";",
                Key.OemQuotes => "'",
                Key.OemComma => ",",
                Key.OemPeriod => ".",
                Key.OemQuestion => "/",
                Key.OemTilde => "`",
                _ => key.ToString()
            };
        }
    }
}
