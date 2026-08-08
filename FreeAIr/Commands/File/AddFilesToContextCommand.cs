using EnvDTE;
using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands.File
{
    /// <summary>
    /// The Solution Explorer menu command that adds the currently selected files to the chat
    /// context of the chat currently selected in the Chat List tool window.
    /// </summary>
    [Command(PackageIds.AddFilesToContextCommandId)]
    internal sealed class AddFilesToContextCommand : BaseCommand<AddFilesToContextCommand>
    {
        /// <summary>
        /// Creates the command instance; no setup is required beyond the base VS command wiring.
        /// </summary>
        public AddFilesToContextCommand(
            )
        {
        }

        /// <summary>
        /// Adds the files selected in Solution Explorer to the currently selected chat's context
        /// and brings that chat's window to the front.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var viewModel = ChatListToolWindow.ChatListViewModel;
            var selectedChat = viewModel.SelectedChat.Chat;

            var selectedFiles = await ApplyFileSupportCommand.GetSelectedFilesAsync();

            await ApplyFileSupportCommand.AddFilesToContextAsync(
                selectedChat,
                selectedFiles
                );

            await ChatWindowShower.ShowChatWindowAsync(selectedChat);
        }

        /// <summary>
        /// Enables the command only when the Chat List tool window is open and has a chat selected.
        /// </summary>
        protected override void BeforeQueryStatus(EventArgs e)
        {
            if (ChatListToolWindow.ChatListViewModel is null)
            {
                this.Command.Enabled = false;
                return;
            }
            if (ChatListToolWindow.ChatListViewModel.SelectedChat is null)
            {
                this.Command.Enabled = false;
                return;
            }

            this.Command.Enabled = true;
        }
    }
}
