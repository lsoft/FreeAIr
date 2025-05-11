using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
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

        private ImageSource? ConvertMonikerToImageSource(ImageMoniker imageMoniker)
        {
            var imageService = ServiceProvider.GlobalProvider.GetService(typeof(SVsImageService)) as IVsImageService2;
            if (imageService == null || imageMoniker.IsNullImage())
            {
                return null;
            }

            var imageAttributes = new ImageAttributes
            {
                Flags = (uint)_ImageAttributesFlags.IAF_RequiredFlags,
                Format = (uint)_UIDataFormat.DF_WPF,
                ImageType = (uint)_UIImageType.IT_Bitmap,
                LogicalWidth = 16,
                LogicalHeight = 16,
                StructSize = Marshal.SizeOf<ImageAttributes>()
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

            return bitmapSource as BitmapSource;
        }


    }
}