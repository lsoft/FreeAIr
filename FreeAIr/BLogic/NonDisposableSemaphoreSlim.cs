using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    /// <summary>
    /// A <see cref="SemaphoreSlim"/> with the disposal taken away.
    ///
    /// Disposing a semaphore while somebody is still waiting on it does not wake that waiter — it
    /// leaves the wait to throw an ObjectDisposedException from somewhere deep inside an async
    /// method, which is a race nobody can win from the outside. Every semaphore in this codebase
    /// lives as long as the object owning it, so the safe answer is to make it impossible to
    /// dispose and let the finalizer of the inner semaphore deal with the handle.
    /// </summary>
    public sealed class NonDisposableSemaphoreSlim
    {
        private readonly SemaphoreSlim _semaphoreSlim;

        public NonDisposableSemaphoreSlim(
            int initialCount
            )
        {
            _semaphoreSlim = new SemaphoreSlim(initialCount);
        }

        public NonDisposableSemaphoreSlim(
            int initialCount,
            int maxCount
            )
        {
            _semaphoreSlim = new SemaphoreSlim(initialCount, maxCount);
        }

        public void Wait() => _semaphoreSlim.Wait();

        public bool Wait(TimeSpan timeout) => _semaphoreSlim.Wait(timeout);

        public Task WaitAsync() => _semaphoreSlim.WaitAsync();

        public Task<bool> WaitAsync(TimeSpan timeout) => _semaphoreSlim.WaitAsync(timeout);

        public void Release() => _semaphoreSlim.Release();
    }
}
