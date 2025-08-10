using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text;

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
                await FreeAIr.BLogic.ChatOptions.GetDefaultAsync(chosenAgent)
                );

            await ChatWindowShower.ShowChatWindowAsync(
                chat,
                true
                );
        }

    }
}
