using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands
{
    [Command(PackageIds.OpenChatListToolWindowCommandId)]
    internal sealed class OpenChatListToolWindowCommand : BaseCommand<OpenChatListToolWindowCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            _ = await ChatListToolWindow.ShowAsync();
        }

    }

}
