using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    /// <summary>
    /// What a <see cref="TimeoutEventProxy{TArgs}"/> does with a new event while an older one is
    /// still waiting to be delivered.
    /// </summary>
    public enum ArgsActionKindEnum
    {
        /// <summary>
        /// Throw the pending one away and keep only the new one. Right when the events describe a
        /// state rather than an occurrence — "the answer has changed" is worth reporting once,
        /// however many times it happened.
        /// </summary>
        ReplaceLastArgs,

        /// <summary>
        /// Keep both and deliver them in order. Right when every event matters on its own and
        /// losing one would lose information.
        /// </summary>
        AddToQueue
    }

    /// <summary>
    /// Throttles a stream of events down to something a UI can keep up with: callers raise as often
    /// as they like, subscribers hear from it at most once per timeout.
    ///
    /// The reason it exists is streaming answers. A completion arrives in dozens of chunks a
    /// second, each one a change worth showing, and rendering markdown that fast freezes the chat
    /// window. This sits in between, collapses the flood using the caller's own rule (see
    /// <see cref="ArgsActionKindEnum"/>) and delivers on a timer.
    ///
    /// The background loop starts on the first raise, not in the constructor: an object holding one
    /// of these may well be created and dropped without ever raising anything.
    /// </summary>
    public sealed class TimeoutEventProxy<TArgs> : IAsyncDisposable
        where TArgs : EventArgs
    {
        /// <summary>
        /// The handler shape. Asynchronous, so a subscriber that has to hop to the UI thread can be
        /// awaited instead of being fired and forgotten.
        /// </summary>
        public delegate Task TimeoutEventProxyDelegate(object sender, TArgs args);

        /// <summary>Guards the pending list, which the raising side and the delivering loop both touch.</summary>
        //guards the pending list, which the raising side and the delivering loop both touch
        private readonly NonDisposableSemaphoreSlim _semaphore = new(1);

        /// <summary>Set by <see cref="DisposeAsync"/> to stop the delivery loop.</summary>
        private readonly ManualResetEvent _stopSignal = new(false);

        /// <summary>Skips the wait and delivers now. Set when the queue has grown past one entry.</summary>
        private readonly AutoResetEvent _immediatelySendSignal = new(false);

        /// <summary>How long the delivery loop waits between flushes of the pending events.</summary>
        private readonly int _timeoutMsec;
        /// <summary>The sender passed to every delivered event.</summary>
        private readonly object _sender;

        /// <summary>
        /// Decides what happens to a newly raised event given the one already pending. Supplied by
        /// the owner, because only it knows whether its events are states or occurrences.
        /// </summary>
        private readonly Func<TArgs?, TArgs?, ArgsActionKindEnum> _determineActionPredicate;

        /// <summary>One means the loop has not been started yet; flipped by an interlocked exchange.</summary>
        private int _firstCall = 1;

        /// <summary>The background delivery loop, started lazily by the first <see cref="FireAsync"/> call.</summary>
        private Task? _workingTask;

        /// <summary>What has been raised but not delivered yet. Guarded by the semaphore.</summary>
        private List<TArgs> _argsList = new();

        /// <summary>Raised on a background thread, at most once per timeout per pending item.</summary>
        /// <summary>Fired on a background thread with the throttled, collapsed-or-queued events.</summary>
        public event TimeoutEventProxyDelegate Event;

        /// <summary>Configures the throttling interval, the sender identity delivered with every event, and the collapsing rule for events raised while one is still pending.</summary>
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

        /// <summary>
        /// Raises an event. Returns as soon as it has been recorded — delivery happens later, on
        /// the background loop, so this is safe to call from a tight streaming loop.
        /// </summary>
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

        /// <summary>
        /// Starts the delivery loop, exactly once however many callers race into here at the same
        /// moment.
        /// </summary>
        private void StartWorkingThread()
        {
            if (Interlocked.Exchange(ref _firstCall, 0) == 1)
            {
                _workingTask = Task.Run(DoWorkAsync);
            }
        }

        /// <summary>
        /// The delivery loop: wait for the timeout, for a request to flush early, or for the stop
        /// signal, and deliver whatever has piled up. Runs until disposal.
        /// </summary>
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

        /// <summary>
        /// Delivers everything pending, in order, and empties the queue. Held under the semaphore
        /// throughout, so a subscriber that takes its time also holds up the raising side — which
        /// is the intended back pressure.
        /// </summary>
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

        /// <summary>
        /// Stops the loop and waits for it to unwind. Whatever is still pending is dropped rather
        /// than flushed: disposal happens when the chat is closing, and nobody is left to hear it.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            _stopSignal.Set();
            if (_workingTask is not null)
            {
                await _workingTask;
            }

            _stopSignal.Dispose();
        }

        /// <summary>Invokes <see cref="Event"/> with the given args, if anybody is subscribed.</summary>
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
