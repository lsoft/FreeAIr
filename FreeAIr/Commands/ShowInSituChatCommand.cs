using EnvDTE;
using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands
{
    [Command(PackageIds.ShowInSituChatCommandId)]
    public sealed class ShowInSituChatCommand : CreateOrReuseChatCommand<ShowInSituChatCommand>
    {
        public ShowInSituChatCommand()
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var chat = await CreateOrReuseChatAsync();
            if (chat is null)
            {
                return;
            }

            await ChatWindowShower.ShowChatWindowAsync(chat, true);
        }

    }
}
