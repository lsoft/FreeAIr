using EnvDTE;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using FreeAIr.Chat;

namespace FreeAIr.Commands
{
    [Command(PackageIds.ShowInSituChatCommandId)]
    public sealed class ShowInSituChatCommand : BaseCommand<ShowInSituChatCommand>
    {
        public ShowInSituChatCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                FreeAIr.Resources.Resources.Choose_agent__with_a_non_empty_token
                );
            if (chosenAgent is null)
            {
                return;
            }

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(null),
                null,
                await FreeAIr.Chat.ChatOptions.GetDefaultAsync(chosenAgent)
                );

            await ChatWindowShower.ShowChatWindowAsync(
                chat,
                true
                );
        }

    }
}
