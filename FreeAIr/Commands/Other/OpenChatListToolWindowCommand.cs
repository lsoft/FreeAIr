using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.OpenChatListToolWindowCommandId)]
    internal sealed class OpenChatListToolWindowCommand : BaseCommand<OpenChatListToolWindowCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ExecuteCommandAsync();
        }

        public static async Task ExecuteCommandAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _ = await ChatListToolWindow.ShowAsync();
        }
    }

}
