using EnvDTE;
using FreeAIr.Agents;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Commands.ContextMenu;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Collections.Generic;

namespace FreeAIr.Commands.NLOutlines
{
    [Command(PackageIds.AgentsContextMenuDynamicCommandId)]
    public sealed class AddNaturalLanguageOutlines_ForFile_AgentsContextMenu_DynamicCommand
        : Base_ContextMenu_DynamicCommand<AddNaturalLanguageOutlines_ForFile_AgentsContextMenu_DynamicCommand, AgentContextMenuItem, AgentsContextMenuCommandBridge>
    {
        protected override async Task ExecuteAsync(AgentContextMenuItem menuItem)
        {
            if (!InternalPage.Instance.IsActiveAgentHasToken())
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    Resources.Resources.Code_NoToken
                    );
                return;
            }

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var contextItems = await _bridge.ContextItemsBuilder.CreateContextItemsAsync();
            if (contextItems is null || contextItems.Count == 0)
            {
                return;
            }

            await ShowAsync(menuItem.Agent, contextItems);
        }

        private static async Task ShowAsync(
            Agent agent,
            List<SolutionItemChatContextItem> contextItems
            )
        {
            var pane = await NaturalLanguageOutlinesToolWindow.ShowAsync();
            var toolWindow = pane.Content as NaturalLanguageOutlinesToolWindowControl;
            var viewModel = toolWindow.DataContext as NaturalLanguageOutlinesViewModel;
            viewModel.SetNewChatAsync(
                agent,
                contextItems
                )
                .FileAndForget(nameof(NaturalLanguageOutlinesViewModel.SetNewChatAsync));
        }
    }
}
