using EnvDTE;
using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands
{
    /// <summary>
    /// The editor command that opens the chat window in-situ (docked near the code) for the
    /// current or newly created chat, letting the user converse without leaving the editor context.
    /// </summary>
    [Command(PackageIds.ShowInSituChatCommandId)]
    public sealed class ShowInSituChatCommand : CreateOrReuseChatCommand<ShowInSituChatCommand>
    {
        /// <summary>
        /// Creates the command instance; no setup is required beyond the base VS command wiring.
        /// </summary>
        public ShowInSituChatCommand()
        {
        }

        /// <summary>
        /// Creates or reuses a chat and shows it in-situ near the editor.
        /// </summary>
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
