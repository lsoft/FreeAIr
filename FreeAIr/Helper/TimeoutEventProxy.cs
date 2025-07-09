using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public enum ArgsActionKindEnum
    {
        ReplaceLastArgs,
        AddToQueue
    }

    public sealed class TimeoutEventProxy<TArgs> : IDisposable
        where TArgs : EventArgs
    {
        public delegate void Delegate(object sender, TArgs args);

        private readonly object _locker = new();

        private readonly ManualResetEvent _stopSignal = new(false);
        private readonly AutoResetEvent _immediatelySendSignal = new(false);
        private readonly int _timeoutMsec;
        private readonly object _sender;
        private readonly Func<TArgs?, TArgs?, ArgsActionKindEnum> _determineActionPredicate;

        private int _firstCall = 1;
        private Thread? _workingThread;

        private List<TArgs> _argsList = new();

        public event Delegate Event;

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

        public void Fire(
            TArgs args
            )
        {
            StartWorkingThread();

            lock (_locker)
            {
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
        }

        private void StartWorkingThread()
        {
            if (Interlocked.Exchange(ref _firstCall, 0) == 1)
            {
                _workingThread = new Thread(WorkThread);
                _workingThread.Start();
            }
        }

        private void WorkThread()
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
                    SendAllItems(
                        //true
                        );
                }
                else if (index == 0)
                {
                    //send signal fired; send all args
                    SendAllItems(
                        //false
                        );
                }
                else if (index == 1)
                {
                    //stop signal fired; stop the thread
                    return;
                }
            }
        }

        private void SendAllItems(
            //bool timeout
            )
        {
            lock (_locker)
            {
                //if (_argsList.Count > 0)
                //{
                //    System.IO.File.AppendAllText(
                //        $"C:\\projects\\LLM\\KoboldCpp\\1.txt",
                //        DateTime.Now.ToString()
                //            + "   waitTimeout: "
                //            + timeout.ToString()
                //            + "   argsList Count: "
                //            + _argsList.Count
                //            + Environment.NewLine
                //        );
                //}

                if (_argsList.Count > 0)
                {
                    for (var i = 0; i < _argsList.Count; i++)
                    {
                        FireEvent(_argsList[i]);
                    }

                    _argsList.Clear();
                }
            }
        }

        public void Dispose()
        {
            _stopSignal.Set();
            _workingThread?.Join();

            _stopSignal.Dispose();
            _immediatelySendSignal.Dispose();
        }

        private void FireEvent(
            TArgs args
            )
        {
            var e = Event;
            if (e is not null)
            {
                e(_sender, args);
            }
        }
    }
}
