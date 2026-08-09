using FreeAIr.Helper;
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
}
