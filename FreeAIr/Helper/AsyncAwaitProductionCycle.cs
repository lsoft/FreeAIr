using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public sealed class AsyncAwaitProductionCycle<T>
    {
        private readonly NonDisposableSemaphoreSlim _startSignal = new(0, 1);
        private readonly AsyncAwaitProduct<T> _productSignal = new();


        public async Task<T> StartCycleAndWaitForProductAsync()
        {
            _startSignal.Release();

            var result = await _productSignal.WaitForProductAsync();
            return result;
        }

        public async Task WaitForCycleStartedAsync()
        {
            await _startSignal.WaitAsync();
        }

        public void SetCycleProduct(T product)
        {
            _productSignal.SetProduct(product);
        }
    }
}
