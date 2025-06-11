using EnvDTE;
using FreeAIr.Agents;
using FreeAIr.BLogic.Context.Item;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FreeAIr.Commands.File
{
    [Command(PackageIds.AddNaturalLanguageOutlinesCommandId)]
    public sealed class AddNaturalLanguageOutlinesCommand : BaseCommand<AddNaturalLanguageOutlinesCommand>
    {
        public AddNaturalLanguageOutlinesCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            AgentsContextMenuCommandBridge.Show(
                SelectedSolutionItemsCommandProcessor.Instance,
                InternalPage.Instance.GetAgentCollection()
                );
        }
    }

    public sealed class SelectedSolutionItemsCommandProcessor : CommandProcessor
    {
        public static readonly SelectedSolutionItemsCommandProcessor Instance = new();

        protected override async System.Threading.Tasks.Task<List<SolutionItemChatContextItem>> CreateContextItemsAsync(
            )
        {
            var contextItems = new List<SolutionItemChatContextItem>();

            var sew = await VS.Windows.GetSolutionExplorerWindowAsync();
            var selections = (await sew.GetSelectionAsync()).ToList();
            if (selections.Count == 0)
            {
                return contextItems;
            }

            foreach (var selection in selections)
            {
                var contextItem = new SolutionItemChatContextItem(
                    new UI.Embedillo.Answer.Parser.SelectedIdentifier(
                        selection.FullPath,
                        null
                        ),
                    false,
                    AddLineNumbersMode.RequiredAllInScope
                    );
                contextItems.Add(contextItem);
            }

            return contextItems;
        }

    }
}
