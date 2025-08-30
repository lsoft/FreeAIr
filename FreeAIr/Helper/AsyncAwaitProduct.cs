using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public sealed class AsyncAwaitProduct<T>
    {
        private readonly NonDisposableSemaphoreSlim _signal = new(0, 1);

        public T Product
        {
            get;
            private set;
        }

        public async Task<T> WaitForProductAsync()
        {
            await _signal.WaitAsync();
            return Product;
        }


        public void SetProduct(T product)
        {
            Product = product;

            _signal.Release();
        }

    }
}
