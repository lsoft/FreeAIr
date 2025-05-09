using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System.Windows.Media;

namespace FreeAIr.UI.Embedillo
{
    public class CompletionData : ICompletionData
    {
        private readonly Suggestion _suggestion;

        public string Text => _suggestion.PublicData;

        public CompletionData(Suggestion suggestion)
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
        public ImageSource? Image => null;

        #endregion
    }
}