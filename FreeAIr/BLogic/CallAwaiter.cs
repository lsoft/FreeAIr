using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    public abstract class CallAwaiter<T>
    {
        private readonly NonDisposableSemaphoreSlim _semaphore = new NonDisposableSemaphoreSlim(0, 1);
        private readonly TimeSpan _callTimeout;

        private bool _finished = false;

        protected CallAwaiter(
            TimeSpan callTimeout
            )
        {
            _callTimeout = callTimeout;
        }

        public async Task<T> WaitForCallAsync()
        {
            if (_finished)
            {
                throw new InvalidOperationException("This class has been designed for one-time usage.");
            }

            try
            {
                await PrepareAsync();

                if (!await _semaphore.WaitAsync(_callTimeout))
                {
                    int g = 0;
                }

                return await GetResultAsync();
            }
            finally
            {
                _finished = true;

                await CleanupAsync();
            }
        }

        protected abstract Task PrepareAsync();

        protected abstract Task<T> GetResultAsync();

        protected abstract Task CleanupAsync();

        protected void Fire()
        {
            _semaphore.Release();
        }

        ~CallAwaiter()
        {
            //additional guard against loss of Fire call
            try
            {
                Fire();
            }
            catch
            {
                //we need nothing to do here
            }
        }
    }
}
