using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    /// <summary>
    /// A one-shot handoff of a value between two async parties: one side awaits, the other side
    /// produces, and neither has to know when the other runs.
    ///
    /// This is a `TaskCompletionSource` written by hand, with the difference that setting the value
    /// twice is not an error here — the second value simply overwrites the first and releases the
    /// semaphore again.
    /// </summary>
    public sealed class AsyncAwaitProduct<T>
    {
        /// <summary>Gate the waiter blocks on; starts empty with room for one release per product.</summary>
        //starts empty with room for one: the waiter blocks until a product shows up
        private readonly NonDisposableSemaphoreSlim _signal = new(0, 1);

        /// <summary>
        /// The value handed over, or the default while nothing has been produced yet. Reading it
        /// without awaiting first tells nothing apart.
        /// </summary>
        public T Product
        {
            get;
            private set;
        }

        /// <summary>
        /// Waits until somebody produces a value and returns it. One waiter only — the semaphore
        /// releases a single slot per product.
        /// </summary>
        public async Task<T> WaitForProductAsync()
        {
            await _signal.WaitAsync();
            return Product;
        }

        /// <summary>
        /// Hands the value over and wakes the waiter. Returns at once; nobody has to be waiting yet.
        /// </summary>
        public void SetProduct(T product)
        {
            //the field is written before the release, so the waiter cannot wake up onto a value
            //which is not there yet
            Product = product;

            _signal.Release();
        }

    }
}
