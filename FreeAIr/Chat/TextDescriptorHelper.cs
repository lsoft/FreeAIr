using System.Threading.Tasks;
using FreeAIr.Chat;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Builds an <see cref="IOriginalTextDescriptor"/> from the currently active editor document,
    /// capturing the selected text span and line ending style so chat context and edits can
    /// target the right part of the file.
    /// </summary>
    public static class TextDescriptorHelper
    {
        /// <summary>
        /// Captures the active editor document, its current selection (if any) and its line
        /// ending, or <c>null</c> when no text editor is currently active.
        /// </summary>
        public static async Task<IOriginalTextDescriptor?> GetSelectedTextAsync()
        {
            var docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView == null)
            {
                //not a text window
                return null;
            }

            FreeAIr.UI.Embedillo.Answer.Parser.SelectedSpan? selected = null;
            var selection = docView.TextView.Selection;
            if (!selection.IsEmpty)
            {
                selected = new UI.Embedillo.Answer.Parser.SelectedSpan(
                    selection.Start.Position.Position,
                    selection.End.Position.Position - selection.Start.Position.Position
                    );
            }

            var lineEnding = LineEndingHelper.Actual.GetActiveDocumentLineEnding();

            return new SelectedTextDescriptor(
                docView,
                selected,
                lineEnding
                );
        }
    }
}
