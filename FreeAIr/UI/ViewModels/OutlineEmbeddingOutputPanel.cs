using FreeAIr.Helper;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.UI.ViewModels
{
    public static class OutlineEmbeddingOutputPanel
    {
        private static readonly NonDisposableSemaphoreSlim _semaphore = new (1, 1);
        private static OutputWindowPane _outlineEmbeddingOutputPanel;

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
