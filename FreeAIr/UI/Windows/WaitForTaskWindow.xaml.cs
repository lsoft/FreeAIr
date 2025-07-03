using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace FreeAIr.UI.Windows
{
    public partial class WaitForTaskWindow : Window
    {
        private readonly BackgroundTask _backgroundTask;

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

        private async void StatusChangesAsync(string status)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                TaskStatusTextBlock.Text = status;
            }
            catch (Exception excp)
            {
                //todo log
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //todo log

            CloseWindowAsync()
                .FileAndForget(nameof(CloseWindowAsync));
        }

        private async Task CloseWindowAsync()
        {
            await _backgroundTask.StopAsync();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _backgroundTask.WaitForCompleteAsync();

            this.Close();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }

    public abstract class BackgroundTask
    {
        protected CancellationTokenSource? _cancellationTokenSource;
        
        private Task? _workingTask;

        public abstract string TaskDescription
        {
            get;
        }

        public string CurrentStatus
        {
            get;
            private set;
        }

        public event ShowStatusDelegate ShowStatusEvent;

        public BackgroundTask(
            )
        {
            _cancellationTokenSource = new();
        }



        protected void StartAsyncTask()
        {
            _workingTask = RunWorkingTaskAsync();
        }

        protected abstract Task RunWorkingTaskAsync();

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
                    //todo log
                }
            }
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource.Cancel();

            await WaitForCompleteAsync();

            _cancellationTokenSource.Dispose();
        }

        protected void SetNewStatus(string status)
        {
            CurrentStatus = status;
            ShowStatusEvent?.Invoke(status);
        }
    }

    public delegate void ShowStatusDelegate(string status);

}
