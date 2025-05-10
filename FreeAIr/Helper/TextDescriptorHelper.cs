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

            var lineEnding = LineEndingHelper.Actual.GetActiveDocumentLineEnding();

            return new SelectedTextDescriptor(
                docView,
                lineEnding
                );
        }
    }
}
