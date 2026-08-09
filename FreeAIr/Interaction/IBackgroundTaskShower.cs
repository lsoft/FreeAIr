using FreeAIr.UI.Windows;

namespace FreeAIr.Interaction
{
    /// <summary>
    /// Runs a <see cref="BackgroundTask"/> in front of the user, showing its description and live
    /// status until it finishes or the user cancels it. The MEF implementation puts up the modal
    /// wait dialog; the point of the interface is that logic which has work to wait for - the git
    /// diff collector, the MCP server installer - does not have to know a window exists.
    /// </summary>
    public interface IBackgroundTaskShower
    {
        /// <summary>
        /// Shows the task's progress and returns once it has completed or been cancelled. The task
        /// itself is already running: inspect its own result afterwards to learn what came of it.
        /// </summary>
        Task ShowAsync(BackgroundTask backgroundTask);
    }
}
