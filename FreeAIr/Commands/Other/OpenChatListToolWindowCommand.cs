using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands.Other
{
    /// <summary>
    /// The menu command that opens the Chat List tool window, showing all active and past chats.
    /// </summary>
    [Command(PackageIds.OpenChatListToolWindowCommandId)]
    internal sealed class OpenChatListToolWindowCommand : BaseCommand<OpenChatListToolWindowCommand>
    {
        /// <summary>
        /// Shows the Chat List tool window when the user invokes the command.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ExecuteCommandAsync();
        }

        /// <summary>
        /// Switches to the UI thread and shows the Chat List tool window; callable directly by
        /// other commands that need to surface it without going through the menu.
        /// </summary>
        public static async Task ExecuteCommandAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _ = await ChatListToolWindow.ShowAsync();
        }
    }

}
