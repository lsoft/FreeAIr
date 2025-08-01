using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FreeAIr.UI.Embedillo
{
    public class CompletionData : ICompletionData
    {
        private readonly ISuggestion _suggestion;

        public string Text => _suggestion.PublicData;

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

        public object Content => Text;
        public object? Description => null;
        public double Priority => 0;
        public ImageSource? Image => ConvertMonikerToImageSource(
            _suggestion.Image
            );

        #endregion

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