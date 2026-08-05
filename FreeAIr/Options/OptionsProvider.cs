using System.Runtime.InteropServices;

namespace FreeAIr
{
    /// <summary>
    /// Registers FreeAIr's Tools &gt; Options pages with Visual Studio by exposing one
    /// <see cref="BaseOptionPage{T}"/> wrapper per options model (recording, UI, font sizes, internal).
    /// </summary>
    internal partial class OptionsProvider
    {
        /// <summary>
        /// Options page wrapper that exposes <see cref="RecordingPage"/> (voice recording and
        /// transcription settings) in the Visual Studio Options dialog.
        /// </summary>
        [ComVisible(true)]
        public class RecordingPageOptions : BaseOptionPage<RecordingPage>
        {
        }

        /// <summary>
        /// Options page wrapper that exposes <see cref="UIPage"/> (chat window layout and behavior
        /// settings) in the Visual Studio Options dialog.
        /// </summary>
        [ComVisible(true)]
        public class UIPageOptions : BaseOptionPage<UIPage>
        {
        }

        /// <summary>
        /// Options page wrapper that exposes <see cref="FontSizePage"/> (chat UI font size settings)
        /// in the Visual Studio Options dialog.
        /// </summary>
        [ComVisible(true)]
        public class FontSizePageOptions : BaseOptionPage<FontSizePage>
        {
        }

        /// <summary>
        /// Options page wrapper that exposes <see cref="InternalPage"/> (internal/diagnostic settings)
        /// in the Visual Studio Options dialog.
        /// </summary>
        [ComVisible(true)]
        public class InternalPageOptions : BaseOptionPage<InternalPage>
        {
        }
    }
}
