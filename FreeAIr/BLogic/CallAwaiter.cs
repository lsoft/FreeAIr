using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    /// <summary>
    /// Turns a callback-driven API into an awaitable one: something else prepares an operation,
    /// eventually calls <see cref="Fire"/> from wherever the callback lands, and the caller of
    /// <see cref="WaitForCallAsync"/> gets the result back as a normal awaited value.
    ///
    /// Built for exactly one call — a second use throws, since there is nothing left to wait for.
    /// The finalizer releases the semaphore as a last-resort guard so a leaked instance whose
    /// callback never fires does not deadlock a waiter forever.
    /// </summary>
    public abstract class CallAwaiter<T>
    {
        /// <summary>Released by <see cref="Fire"/> to wake whoever is inside <see cref="WaitForCallAsync"/>.</summary>
        private readonly NonDisposableSemaphoreSlim _semaphore = new NonDisposableSemaphoreSlim(0, 1);
        /// <summary>How long <see cref="WaitForCallAsync"/> waits for <see cref="Fire"/> before giving up.</summary>
        private readonly TimeSpan _callTimeout;

        /// <summary>Set once <see cref="WaitForCallAsync"/> has run, so a second call can be rejected.</summary>
        private bool _finished = false;

        /// <summary>Sets the timeout <see cref="WaitForCallAsync"/> will wait for the callback to fire.</summary>
        protected CallAwaiter(
            TimeSpan callTimeout
            )
        {
            _callTimeout = callTimeout;
        }

        /// <summary>
        /// Runs prepare → wait for <see cref="Fire"/> (or the timeout) → collect the result,
        /// cleaning up unconditionally afterwards even if any step throws.
        /// </summary>
        public async Task<T> WaitForCallAsync()
        {
            if (_finished)
            {
                throw new InvalidOperationException("This class has been designed for one-time usage.");
            }

            try
            {
                await PrepareAsync();

                _ = await _semaphore.WaitAsync(_callTimeout);

                return await GetResultAsync();
            }
            finally
            {
                _finished = true;

                await CleanupAsync();
            }
        }

        /// <summary>Starts whatever operation the callback will eventually complete.</summary>
        protected abstract Task PrepareAsync();

        /// <summary>Reads the result out after the callback has fired.</summary>
        protected abstract Task<T> GetResultAsync();

        /// <summary>Releases whatever <see cref="PrepareAsync"/> acquired, called even on failure or timeout.</summary>
        protected abstract Task CleanupAsync();

        /// <summary>Called from the callback to release the waiter in <see cref="WaitForCallAsync"/>.</summary>
        protected void Fire()
        {
            _semaphore.Release();
        }

        /// <summary>Last-resort guard: releases the semaphore so a leaked instance whose callback never fired does not deadlock a waiter forever.</summary>
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
