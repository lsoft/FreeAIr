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

        public Task WaitAsync() => _semaphoreSlim.WaitAsync();

        public void Release() => _semaphoreSlim.Release();
    }
}
