using EnvDTE;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ViewModels;
using System.Collections.Generic;
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

        protected override void BeforeQueryStatus(EventArgs e)
        {
            this.Command.Enabled = DTEHelper.CheckIfOnlySolutionSelected();
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var chosenSupportAction = await SupportContextMenu.ChooseSupportAsync(
                "Choose support action:",
                SupportScopeEnum.GenerateNaturalLanguageOutlines
                );
            if (chosenSupportAction is null)
            {
                return;
            }

            var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                "Choose agent to add NL outlines:",
                chosenSupportAction.AgentName
                );
            if (chosenAgent is null)
            {
                return;
            }

            var chosenSolutionItems = await CreateChosenSolutionItemsAsync();

            await NaturalLanguageOutlinesViewModel.ShowPanelAsync(
                chosenSupportAction,
                chosenAgent,
                chosenSolutionItems
                );
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
