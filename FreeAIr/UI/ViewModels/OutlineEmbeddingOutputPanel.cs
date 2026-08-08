using System.Threading.Tasks;
using FreeAIr.BLogic;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// Owns the Visual Studio Output window pane ("FreeAIr NLO Json file generation") that the
    /// natural-language-outline embedding pipeline writes its progress and diagnostics to.
    /// </summary>
    public static class OutlineEmbeddingOutputPanel
    {
        /// <summary>
        /// Serializes creation of the shared output pane so concurrent callers do not create it twice.
        /// </summary>
        private static readonly NonDisposableSemaphoreSlim _semaphore = new (1, 1);
        /// <summary>
        /// The lazily created Output window pane, reused for the lifetime of the IDE session.
        /// </summary>
        private static OutputWindowPane _outlineEmbeddingOutputPanel;

        /// <summary>
        /// Returns the NLO embedding output pane, creating it in the Visual Studio Output window
        /// on first use.
        /// </summary>
        public static async Task<OutputWindowPane> CreateOrGetAsync()
        {
            try
            {
                await _semaphore.WaitAsync();

                if (_outlineEmbeddingOutputPanel is null)
                {
                    _outlineEmbeddingOutputPanel = await VS.Windows.CreateOutputWindowPaneAsync(
                        FreeAIr.Resources.Resources.FreeAIr_NLO_Json_file_generation
                        );
                }
            }
            finally
            {
                _semaphore.Release();
            }

            return _outlineEmbeddingOutputPanel;
        }
    }
}
