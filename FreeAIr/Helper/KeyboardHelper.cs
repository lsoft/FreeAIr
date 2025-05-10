using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace FreeAIr.Helper
{
    public static class KeyboardHelper
    {
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

        public sealed class TextChangingKeyInfo
        {
            private readonly bool _postProcessCopyToClipboard;

            public bool IsTextChangedCombination
            {
                get;
            }
            public string? EnteredText
            {
                get;
            }

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

        public enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        #endregion

        public static string GetStringFromKey(this Key key)
        {
            return GetCharFromKey(key).ToString();
        }

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
