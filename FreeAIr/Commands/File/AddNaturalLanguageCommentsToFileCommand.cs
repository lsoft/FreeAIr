using EnvDTE;
using FreeAIr.Agents;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Commands.ContextMenu;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Linq;
using static FreeAIr.Agents.AgentsContextMenuCommandBridge;
using static FreeAIr.Helper.SolutionHelper;

namespace FreeAIr.Commands.File
{
    [Command(PackageIds.AddNaturalLanguageCommentsCommandId)]
    public sealed class AddNaturalLanguageCommentsCommand : BaseCommand<AddNaturalLanguageCommentsCommand>
    {
        public AddNaturalLanguageCommentsCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            AgentsContextMenuCommandBridge.Show(
                InternalPage.Instance.GetAgentCollection()
                );
        }
    }


    [Command(PackageIds.AgentsContextMenuDynamicCommandId)]
    public sealed class AddNaturalLanguageComments_AgentsContextMenu_DynamicCommand
        : Base_ContextMenu_DynamicCommand<AddNaturalLanguageComments_AgentsContextMenu_DynamicCommand, AgentContextMenuItem, AgentsContextMenuCommandBridge>
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

            var sew = await VS.Windows.GetSolutionExplorerWindowAsync();
            var selections = (await sew.GetSelectionAsync()).ToList();
            if (selections.Count == 0)
            {
                return;
            }


            var pane = await NaturalLanguageOutlinesToolWindow.ShowAsync();
            var toolWindow = pane.Content as NaturalLanguageOutlinesToolWindowControl;
            var viewModel = toolWindow.DataContext as NaturalLanguageOutlinesViewModel;
            viewModel.SetNewChatAsync(
                menuItem.Agent,
                selections.ConvertAll(s => new FoundSolutionItem(s, null))
                )
                .FileAndForget(nameof(NaturalLanguageOutlinesViewModel.SetNewChatAsync));
        }
    }
}
