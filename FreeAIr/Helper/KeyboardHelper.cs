using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Helper for classifying keyboard input while the user is typing a chat prompt: determines
    /// whether a key press or key combination changes the edited text (and what text it inserts),
    /// and provides low-level virtual-key-to-character translation via user32.dll.
    /// </summary>
    public static class KeyboardHelper
    {
        /// <summary>
        /// Classifies a key press together with its modifiers, deciding whether it changes the text
        /// being edited (e.g. typing a character, backspace, paste) and what text it would insert.
        /// Used to keep prompt-input tracking in sync with what the user actually typed.
        /// </summary>
        public static TextChangingKeyInfo IsTextChangedCombination(
            this Key key,
            ModifierKeys modifiers
            )
        {
            if (modifiers == ModifierKeys.Windows) //именно так проверять надо!
            {
                //вроде бы никакие сочетания с Windows не меняют текст
                return new(false, string.Empty);
            }
            if (modifiers.HasFlag(ModifierKeys.Alt)) //именно так проверять надо!
            {
                //вроде бы никакие сочетания с Alt не меняют текст
                return new(false, string.Empty);
            }

            var ctrl = modifiers.HasFlag(ModifierKeys.Control);
            //var alt = modifiers.HasFlag(ModifierKeys.Alt);
            var shift = modifiers.HasFlag(ModifierKeys.Shift);
            //var windows = modifiers.HasFlag(ModifierKeys.Windows);

            return key switch
            {
                Key.Space => new(true, ""), //пробел считается символом
                Key.Tab => new(true, "\t"), //табуляция тоже меняет текст, это символ
                Key.Back => new(true, string.Empty),
                Key.Delete => new(true, string.Empty),
                Key.Enter => new(true, Environment.NewLine),
                Key.Escape => new(false, string.Empty),
                Key.LeftAlt => new(false, string.Empty),
                Key.RightAlt => new(false, string.Empty),
                Key.LeftCtrl => new(false, string.Empty),
                Key.RightCtrl => new(false, string.Empty),
                Key.LeftShift => new(false, string.Empty),
                Key.RightShift => new(false, string.Empty),
                Key.Home => new(false, string.Empty),
                Key.End => new(false, string.Empty),
                Key.PageUp => new(false, string.Empty),
                Key.PageDown => new(false, string.Empty),
                Key.PrintScreen => new(false, string.Empty),
                Key.Print => new(false, string.Empty),
                Key.LWin => new(false, string.Empty),
                Key.RWin => new(false, string.Empty),
                Key.Scroll => new(false, string.Empty),
                Key.Pause => new(false, string.Empty),
                Key.MediaNextTrack => new(false, string.Empty),
                Key.MediaPlayPause => new(false, string.Empty),
                Key.MediaPreviousTrack => new(false, string.Empty),
                Key.MediaStop => new(false, string.Empty),
                Key.SelectMedia => new(false, string.Empty),
                Key.NumLock => new(false, string.Empty),
                Key.Apps => new(false, string.Empty),
                Key.C => new(!ctrl, (ctrl ? string.Empty : key.GetStringFromKey()) ), //ctrl+c (копирование) не меняет состояние текста
                Key.Insert => new(shift, (shift ? Clipboard.GetText() : string.Empty), ctrl), //shift+insert это = Ctrl+V, это меняет текст
                Key.V => new(true, (ctrl ? Clipboard.GetText() : key.GetStringFromKey())), //shift+insert это = Ctrl+V, это меняет текст
                Key.X => new(true, (ctrl ? string.Empty : key.GetStringFromKey()), true), //shift+insert это = Ctrl+V, это меняет текст
                _ when key >= Key.F1 && key <= Key.F24 => new(false, string.Empty), // F1-F24
                _ when key >= Key.Left && key <= Key.Down => new(false, string.Empty), // стрелки
                _ => new(true, key.GetStringFromKey())
            };
        }

        /// <summary>
        /// Result of classifying a key press: whether it changes the edited text, what text it
        /// inserts (if any), and whether the caller should additionally copy text to the clipboard
        /// afterward (used for cut/copy shortcuts).
        /// </summary>
        public sealed class TextChangingKeyInfo
        {
            private readonly bool _postProcessCopyToClipboard;

            /// <summary>
            /// Whether the classified key combination changes the text being edited.
            /// </summary>
            public bool IsTextChangedCombination
            {
                get;
            }
            /// <summary>
            /// The text the key combination would insert, or null/empty when it does not insert text.
            /// </summary>
            public string? EnteredText
            {
                get;
            }

            /// <summary>
            /// Creates a classification result for a key combination.
            /// </summary>
            public TextChangingKeyInfo(
                bool isTextChangedCombination,
                string? enteredText,
                bool postProcessCopyToClipboard = false
                )
            {
                IsTextChangedCombination = isTextChangedCombination;
                EnteredText = enteredText;
                _postProcessCopyToClipboard = postProcessCopyToClipboard;
            }

            /// <summary>
            /// Copies the given text to the system clipboard, but only if this key combination was
            /// classified as one that should trigger a clipboard copy (e.g. Ctrl+X).
            /// </summary>
            public void PostProcessCopyToClipboard(string text)
            {
                if (!_postProcessCopyToClipboard)
                {
                    return;
                }

                Clipboard.SetText(text);
            }

        }

        #region unmanaged

        /// <summary>
        /// Conversion mode passed to the user32.dll <c>MapVirtualKey</c> function, selecting which
        /// direction to translate between virtual key codes and scan codes/characters.
        /// </summary>
        public enum MapType : uint
        {
            /// <summary>Translate a virtual-key code into a scan code.</summary>
            MAPVK_VK_TO_VSC = 0x0,
            /// <summary>Translate a scan code into a virtual-key code.</summary>
            MAPVK_VSC_TO_VK = 0x1,
            /// <summary>Translate a virtual-key code into an unshifted character value.</summary>
            MAPVK_VK_TO_CHAR = 0x2,
            /// <summary>Translate a scan code into a virtual-key code, distinguishing left/right keys.</summary>
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        /// <summary>
        /// P/Invoke wrapper for the Win32 <c>ToUnicode</c> function, translating a virtual key and
        /// current keyboard state into the Unicode character(s) it would produce.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        /// <summary>
        /// P/Invoke wrapper for the Win32 <c>GetKeyboardState</c> function, filling in the current
        /// state of all virtual keys for use by <see cref="ToUnicode"/>.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        /// <summary>
        /// P/Invoke wrapper for the Win32 <c>MapVirtualKey</c> function, translating between
        /// virtual-key codes, scan codes and characters according to <see cref="MapType"/>.
        /// </summary>
        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        #endregion

        /// <summary>
        /// Returns the character a WPF <see cref="Key"/> produces on the current keyboard layout, as a string.
        /// </summary>
        public static string GetStringFromKey(this Key key)
        {
            return GetCharFromKey(key).ToString();
        }

        /// <summary>
        /// Resolves the Unicode character a WPF <see cref="Key"/> produces on the current keyboard
        /// layout, using the Win32 keyboard-state and virtual-key APIs; returns a space if it cannot
        /// be resolved.
        /// </summary>
        public static char GetCharFromKey(this Key key)
        {
            char ch = ' ';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                case 1:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }
    }
}
