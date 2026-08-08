using FreeAIr.UI.ViewModels;
using FreeAIr.UI.Windows;
using System.Windows;

namespace FreeAIr.Commands.Other
{
    /// <summary>
    /// The menu command that opens the Control Center window, the central dashboard for
    /// managing FreeAIr's chats, agents, and background activity.
    /// </summary>
    [Command(PackageIds.OpenControlCenterCommandId)]
    internal sealed class OpenControlCenterCommand : BaseCommand<OpenControlCenterCommand>
    {
        /// <summary>
        /// Shows the Control Center window when the user invokes the command.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await ShowAsync(
                );
        }

        /// <summary>
        /// Builds the Control Center window and its view model and shows it as a modal dialog.
        /// </summary>
        public static async Task ShowAsync(
            )
        {
            var w = new ControlCenterWindow(
                );
            var vm = new ControlCenterViewModel(
                );
            w.DataContext = vm;
            _ = await w.ShowDialogAsync();
        }
    }

}
