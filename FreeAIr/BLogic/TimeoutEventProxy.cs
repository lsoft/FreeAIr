using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    public enum ArgsActionKindEnum
    {
        ReplaceLastArgs,
        AddToQueue
    }

    public sealed class TimeoutEventProxy<TArgs> : IAsyncDisposable
        where TArgs : EventArgs
    {
        public delegate Task TimeoutEventProxyDelegate(object sender, TArgs args);

        private readonly NonDisposableSemaphoreSlim _semaphore = new(1);

        private readonly ManualResetEvent _stopSignal = new(false);
        private readonly AutoResetEvent _immediatelySendSignal = new(false);
        private readonly int _timeoutMsec;
        private readonly object _sender;
        private readonly Func<TArgs?, TArgs?, ArgsActionKindEnum> _determineActionPredicate;

        private int _firstCall = 1;
        private Task? _workingTask;

        private List<TArgs> _argsList = new();

        public event TimeoutEventProxyDelegate Event;

        public TimeoutEventProxy(
            int timeoutMsec,
            object sender,
            Func<TArgs?, TArgs?, ArgsActionKindEnum> determineActionPredicate
            )
        {
            if (sender is null)
            {
                throw new ArgumentNullException(nameof(sender));
            }

            if (determineActionPredicate is null)
            {
                throw new ArgumentNullException(nameof(determineActionPredicate));
            }

            _timeoutMsec = timeoutMsec;
            _sender = sender;
            _determineActionPredicate = determineActionPredicate;
        }

        public async Task FireAsync(
            TArgs args
            )
        {
            StartWorkingThread();

            try
            {
                await _semaphore.WaitAsync();

                var action = _determineActionPredicate(_argsList.LastOrDefault(), args);
                switch (action)
                {
                    case ArgsActionKindEnum.ReplaceLastArgs:
                        if (_argsList.Count > 0)
                        {
                            _argsList[_argsList.Count - 1] = args;
                        }
                        break;
                    case ArgsActionKindEnum.AddToQueue:
                        {
                            _argsList.Add(args);

                            if (_argsList.Count > 1)
                            {
                                //no need to fire first args in the queue immediately
                                //store it to the queue and wait for the next args
                                //so we will have base value to compare against new value
                                _immediatelySendSignal.Set();
                            }
                        }
                        break;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void StartWorkingThread()
        {
            if (Interlocked.Exchange(ref _firstCall, 0) == 1)
            {
                _workingTask = Task.Run(DoWorkAsync);
            }
        }

        private async Task DoWorkAsync()
        {
            while (true)
            {
                var index = WaitHandle.WaitAny(
                    [
                        _immediatelySendSignal,
                        _stopSignal
                    ],
                    _timeoutMsec
                    );
                if (index == WaitHandle.WaitTimeout)
                {
                    //timeout! send all args
                    await SendAllItemsAsync(
                        );
                }
                else if (index == 0)
                {
                    //send signal fired; send all args
                    await SendAllItemsAsync(
                        );
                }
                else if (index == 1)
                {
                    //stop signal fired; stop the thread
                    return;
                }
            }
        }

        private async Task SendAllItemsAsync(
            )
        {
            try
            {
                await _semaphore.WaitAsync();

                if (_argsList.Count > 0)
                {
                    for (var i = 0; i < _argsList.Count; i++)
                    {
                        await FireEventAsync(_argsList[i]);
                    }

                    _argsList.Clear();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            _stopSignal.Set();
            if (_workingTask is not null)
            {
                await _workingTask;
            }

            _stopSignal.Dispose();
        }

        private async Task FireEventAsync(
            TArgs args
            )
        {
            var e = Event;
            if (e is not null)
            {
                await e(_sender, args);
            }
        }
    }
}
