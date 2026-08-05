using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FreeAIr.UI.Embedillo
{
    /// <summary>
    /// Adapts an <see cref="ISuggestion"/> mention candidate to AvalonEdit's <see cref="ICompletionData"/>
    /// contract, so it can be shown and inserted by the mention autocomplete popup, including rendering the
    /// suggestion's icon through the Visual Studio image service.
    /// </summary>
    public class CompletionData : ICompletionData
    {
        /// <summary>
        /// The mention candidate this completion entry wraps and inserts when accepted.
        /// </summary>
        private readonly ISuggestion _suggestion;

        /// <summary>
        /// The short, human-readable text shown for this entry in the completion list.
        /// </summary>
        public string Text => _suggestion.PublicData;

        /// <summary>
        /// Wraps the given mention suggestion as a completion entry.
        /// </summary>
        public CompletionData(
            ISuggestion suggestion
            )
        {
            if (suggestion is null)
            {
                throw new ArgumentNullException(nameof(suggestion));
            }

            _suggestion = suggestion;
        }

        /// <summary>
        /// Replaces the partially-typed mention text in the editor with this suggestion's full data (e.g.
        /// the complete file path or identifier) when the user accepts this completion entry.
        /// </summary>
        public void Complete(
            TextArea textArea,
            ISegment completionSegment,
            EventArgs insertionRequestEventArgs
            )
        {
            var fullData = _suggestion.FullData;

            textArea.Document.Remove(completionSegment.Offset, completionSegment.Length);
            textArea.Document.Insert(completionSegment.Offset, fullData);
        }

        #region Реализация остальных членов ICompletionData

        /// <summary>
        /// The content displayed for this entry in the completion list; same as <see cref="Text"/>.
        /// </summary>
        public object Content => Text;

        /// <summary>
        /// Extended description shown for this entry; unused, always null.
        /// </summary>
        public object? Description => null;

        /// <summary>
        /// Sort priority among competing completion entries; unused, always zero.
        /// </summary>
        public double Priority => 0;

        /// <summary>
        /// The icon shown next to this entry, rendered from the suggestion's image moniker via the VS image
        /// service.
        /// </summary>
        public ImageSource? Image => ConvertMonikerToImageSource(
            _suggestion.Image
            );

        #endregion

        /// <summary>
        /// Renders a Visual Studio <see cref="ImageMoniker"/> (the icon catalog used throughout the VS
        /// shell) into a WPF <see cref="ImageSource"/> themed to match the current tool window background,
        /// so mention/completion icons look native inside the chat editor.
        /// </summary>
        public static ImageSource? ConvertMonikerToImageSource(
            ImageMoniker imageMoniker,
            int? width = null,
            int? height = null
            )
        {
            var imageService = ServiceProvider.GlobalProvider.GetService(typeof(SVsImageService)) as IVsImageService2;
            if (imageService == null || imageMoniker.IsNullImage())
            {
                return null;
            }

            var backgroundColor = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
            uint ColorToColorRef(System.Drawing.Color color)
            {
                return (uint)(
                    (color.A << 24) |
                    (color.B << 16) |
                    (color.G << 8) |
                    (color.R)
                );
            }

            var imageAttributes = new ImageAttributes
            {
                Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags | 0x8000_0000/*this is _ImageAttributesFlags.IAF_Background*/,
                Format = (uint)_UIDataFormat.DF_WPF,
                ImageType = (uint)_UIImageType.IT_Bitmap,
                LogicalWidth = Math.Max(width.GetValueOrDefault(16), 16),
                LogicalHeight = Math.Max(height.GetValueOrDefault(16), 16),
                Dpi = 96,
                StructSize = Marshal.SizeOf<ImageAttributes>(),
                Background = ColorToColorRef(backgroundColor)
            };

            var bitmapFrame = imageService.GetImage(imageMoniker, imageAttributes);
            if (bitmapFrame == null)
            {
                return null;
            }

            object bitmapSource;
            bitmapFrame.get_Data(out bitmapSource);

            if (bitmapSource == null)
            {
                return null;
            }

            var result = bitmapSource as BitmapSource;

            return result;
        }



    }
}