using FreeAIr.Helper;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace FreeAIr.UI.Windows
{
    /// <summary>
    /// Modal dialog that shows the live status of a <see cref="BackgroundTask"/> (e.g. the
    /// GitHub MCP server install) while it runs, and stops the task if the window is closed or
    /// cancelled before it finishes.
    /// </summary>
    public partial class WaitForTaskWindow : Window
    {
        /// <summary>
        /// The background task whose status is displayed and which is stopped when the window closes early.
        /// </summary>
        private readonly BackgroundTask _backgroundTask;

        /// <summary>
        /// Creates the window bound to the given background task, subscribing to its status
        /// updates and showing its description immediately.
        /// </summary>
        public WaitForTaskWindow(
            BackgroundTask backgroundTask
            )
        {
            if (backgroundTask is null)
            {
                throw new ArgumentNullException(nameof(backgroundTask));
            }

            InitializeComponent();

            _backgroundTask = backgroundTask;
            _backgroundTask.ShowStatusEvent += StatusChangesAsync;

            StatusChangesAsync(_backgroundTask.CurrentStatus);

            TaskDescriptionTextBlock.Text = backgroundTask.TaskDescription;
        }

        /// <summary>
        /// Updates the status text on the main thread whenever the background task reports new progress.
        /// </summary>
        private async void StatusChangesAsync(string status)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                TaskStatusTextBlock.Text = status;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Closes the window when the user clicks Cancel; closing triggers <see cref="Window_Closing"/>
        /// which actually stops the background task.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Requests the background task stop when the window is closing for any reason (cancel,
        /// escape, or the task finishing on its own).
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //todo log

            CloseWindowAsync()
                .FileAndForget(nameof(CloseWindowAsync));
        }

        /// <summary>
        /// Stops the background task and waits for it to actually finish before the window is gone.
        /// </summary>
        private async Task CloseWindowAsync()
        {
            await _backgroundTask.StopAsync();
        }

        /// <summary>
        /// Waits for the background task to complete on its own, then closes the window.
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _backgroundTask.WaitForCompleteAsync();

            this.Close();
        }

        /// <summary>
        /// Closes the window when the user presses Escape.
        /// </summary>
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }

    /// <summary>
    /// Base class for a long-running operation shown in a <see cref="WaitForTaskWindow"/>: runs
    /// its work on a background task, reports status text through <see cref="ShowStatusEvent"/>,
    /// and supports cooperative cancellation via <see cref="StopAsync"/>.
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
        }

        /// <summary>
        /// Raised whenever the task's status text changes, so the wait window can update its display.
        /// </summary>
        public event ShowStatusDelegate ShowStatusEvent;

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
            _cancellationTokenSource.Cancel();

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
