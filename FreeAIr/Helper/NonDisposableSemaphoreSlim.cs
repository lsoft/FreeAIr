using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
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
