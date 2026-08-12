using FreeAIr.Helper;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.UI.Windows
{
    /// <summary>
    /// Base class for a long-running operation shown in a wait dialog: runs its work on a
    /// background task, reports status text through <see cref="ShowStatusEvent"/>, and supports
    /// cooperative cancellation via <see cref="StopAsync"/>. There is no UI in here - the dialog
    /// is one possible presenter of it, which is what lets git and MCP code derive from this
    /// without reaching into the editor.
    /// </summary>
    public abstract class BackgroundTask
    {
        /// <summary>
        /// Token source cancelled when the task is stopped; derived classes observe its token to
        /// abort their work cooperatively.
        /// </summary>
        protected CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// The task returned by <see cref="RunWorkingTaskAsync"/> once started, tracked so it can
        /// be awaited by <see cref="WaitForCompleteAsync"/>.
        /// </summary>
        private Task? _workingTask;

        /// <summary>
        /// Human-readable description of what this task is doing, shown at the top of the wait window.
        /// </summary>
        public abstract string TaskDescription
        {
            get;
        }

        /// <summary>
        /// The most recent status text reported by the task.
        /// </summary>
        public string CurrentStatus
        {
            get;
            private set;
        } = string.Empty;

        /// <summary>
        /// Raised whenever the task's status text changes, so the wait window can update its display.
        /// </summary>
        public event ShowStatusDelegate? ShowStatusEvent;

        /// <summary>
        /// Creates the task and its cancellation token source.
        /// </summary>
        public BackgroundTask(
            )
        {
            _cancellationTokenSource = new();
        }



        /// <summary>
        /// Starts the derived class's work and tracks it as the task to await/cancel.
        /// </summary>
        protected void StartAsyncTask()
        {
            _workingTask = RunWorkingTaskAsync();
        }

        /// <summary>
        /// Performs the actual background work; implementations should observe
        /// <see cref="_cancellationTokenSource"/>'s token to support cancellation.
        /// </summary>
        protected abstract Task RunWorkingTaskAsync();

        /// <summary>
        /// Awaits the working task if it is still running, swallowing cancellation and logging
        /// any other failure instead of letting it escape.
        /// </summary>
        public async Task WaitForCompleteAsync()
        {
            if (_workingTask is not null
                && !(_workingTask.IsCompleted || _workingTask.IsCanceled || _workingTask.IsFaulted)
                )
            {
                try
                {
                    await _workingTask;
                }
                catch (OperationCanceledException)
                {
                    //nothing to do, this is ok
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();
                }
            }
        }

        /// <summary>
        /// Signals cancellation, waits for the working task to actually stop, and disposes the
        /// cancellation token source.
        /// </summary>
        public async Task StopAsync()
        {
            //the constructor always creates one, and nothing but this method disposes it
            _cancellationTokenSource!.Cancel();

            await WaitForCompleteAsync();

            _cancellationTokenSource.Dispose();
        }

        /// <summary>
        /// Records the given status text and raises <see cref="ShowStatusEvent"/> so the wait
        /// window updates its display.
        /// </summary>
        protected void SetNewStatus(string status)
        {
            CurrentStatus = status;
            ShowStatusEvent?.Invoke(status);
        }
    }

    /// <summary>
    /// Delegate for <see cref="BackgroundTask.ShowStatusEvent"/>, carrying the task's latest status text.
    /// </summary>
    public delegate void ShowStatusDelegate(string status);
}
