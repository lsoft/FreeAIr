using FreeAIr.BLogic;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class TextDescriptorHelper
    {
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
