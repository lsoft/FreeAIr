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
        /// <summary>The real semaphore doing the work; never disposed, left for its finalizer to reclaim.</summary>
        private readonly SemaphoreSlim _semaphoreSlim;

        /// <summary>Creates the semaphore with an initial count and no upper bound.</summary>
        public NonDisposableSemaphoreSlim(
            int initialCount
            )
        {
            _semaphoreSlim = new SemaphoreSlim(initialCount);
        }

        /// <summary>Creates the semaphore with an initial count and a maximum count.</summary>
        public NonDisposableSemaphoreSlim(
            int initialCount,
            int maxCount
            )
        {
            _semaphoreSlim = new SemaphoreSlim(initialCount, maxCount);
        }

        /// <summary>Blocks the calling thread until a slot is available.</summary>
        public void Wait() => _semaphoreSlim.Wait();

        /// <summary>Blocks the calling thread until a slot is available or the timeout elapses.</summary>
        public bool Wait(TimeSpan timeout) => _semaphoreSlim.Wait(timeout);

        /// <summary>Asynchronously waits until a slot is available.</summary>
        public Task WaitAsync() => _semaphoreSlim.WaitAsync();

        /// <summary>Asynchronously waits until a slot is available or the timeout elapses.</summary>
        public Task<bool> WaitAsync(TimeSpan timeout) => _semaphoreSlim.WaitAsync(timeout);

        /// <summary>Releases one slot, waking a waiter if one is blocked.</summary>
        public void Release() => _semaphoreSlim.Release();
    }
}
