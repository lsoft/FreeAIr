using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    /// <summary>
    /// A rendezvous between a caller who wants something done and a long-lived worker loop that
    /// does it: the caller says "go" and waits for the result, the worker waits for "go" and
    /// answers with one.
    ///
    /// Used by <see cref="RecorderTranscriberPostProcessor"/>, where the worker is the recording
    /// pipeline: it sits idle until the user presses record, then hands back the transcription. Two
    /// signals rather than one because the worker must not miss a start that arrived while it was
    /// still finishing the previous round.
    /// </summary>
    public sealed class AsyncAwaitProductionCycle<T>
    {
        private readonly NonDisposableSemaphoreSlim _startSignal = new(0, 1);
        private readonly AsyncAwaitProduct<T> _productSignal = new();

        /// <summary>
        /// Called by the requesting side: releases the worker and waits for whatever it produces.
        /// </summary>
        public async Task<T> StartCycleAndWaitForProductAsync()
        {
            _startSignal.Release();

            var result = await _productSignal.WaitForProductAsync();
            return result;
        }

        /// <summary>
        /// Called by the worker at the top of its loop: blocks until somebody asks for a round.
        /// </summary>
        public async Task WaitForCycleStartedAsync()
        {
            await _startSignal.WaitAsync();
        }

        /// <summary>
        /// Called by the worker when the round is done, which is what lets the requester return.
        /// </summary>
        public void SetCycleProduct(T product)
        {
            _productSignal.SetProduct(product);
        }
    }
}
