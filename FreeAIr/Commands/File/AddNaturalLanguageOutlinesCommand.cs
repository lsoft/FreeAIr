using EnvDTE;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.NLOutline;
using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FreeAIr.Commands.File
{
    [Command(PackageIds.AddNaturalLanguageOutlinesCommandId)]
    public sealed class AddNaturalLanguageOutlinesCommand
        : BaseCommand<AddNaturalLanguageOutlinesCommand>
    {
        public AddNaturalLanguageOutlinesCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var chosenAgent = await VisualStudioContextMenuCommandBridge.ShowAsync<AgentJson>(
                "Choose agent to add NL outlines:",
                (await FreeAIrOptions.DeserializeAgentCollectionAsync()).Agents.ConvertAll(a => (a.Name, a as object))
                );
            if (chosenAgent is null)
            {
                return;
            }

            var chosenSolutionItems = await CreateChosenSolutionItemsAsync();

            await NaturalLanguageOutlinesViewModel.ShowPanelAsync(chosenAgent, chosenSolutionItems);
        }

        private async System.Threading.Tasks.Task<List<SolutionItemChatContextItem>> CreateChosenSolutionItemsAsync(
            )
        {
            var foundItems = await SolutionHelper.ProcessDownRecursivelyForSelectedAsync(
                item => !item.IsNonVisibleItem && item.Type == SolutionItemType.PhysicalFile && FileTypeHelper.GetFileType(item.FullPath) == FileTypeEnum.Text,
                false,
                CancellationToken.None
                );
            var result = foundItems.ConvertAll(i =>
                new SolutionItemChatContextItem(
                    new UI.Embedillo.Answer.Parser.SelectedIdentifier(
                        i.SolutionItem.FullPath,
                        null
                        ),
                    false,
                    AddLineNumbersMode.RequiredAllInScope
                    )
                );
            return result;
        }

    }
}
