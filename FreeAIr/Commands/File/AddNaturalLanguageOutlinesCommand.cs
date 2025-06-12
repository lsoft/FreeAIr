using EnvDTE;
using FreeAIr.Agents;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
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
