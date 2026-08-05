using EnvDTE;
using FreeAIr.Helper;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Embedillo.Answer.Parser;
using FreeAIr.UI.ViewModels;
using System.Collections.Generic;
using System.Threading;
using FreeAIr.Chat;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.Commands.File
{
    /// <summary>
    /// The Solution Explorer menu command that kicks off generation of natural language outlines
    /// (the comments the NLO/RAG index searches over) for the selected text files, via the
    /// "Building Natural Language Outlines" panel.
    /// </summary>
    [Command(PackageIds.AddNaturalLanguageOutlinesCommandId)]
    public sealed class AddNaturalLanguageOutlinesCommand : BaseCommand<AddNaturalLanguageOutlinesCommand>
    {
        /// <summary>
        /// Creates the command instance; no setup is required beyond the base VS command wiring.
        /// </summary>
        public AddNaturalLanguageOutlinesCommand(
            )
        {
        }

        /// <summary>
        /// Enables the command only when the whole solution (not an individual item) is selected
        /// in Solution Explorer.
        /// </summary>
        protected override void BeforeQueryStatus(EventArgs e)
        {
            this.Command.Enabled = DTEHelper.CheckIfOnlySolutionSelected();
        }

        /// <summary>
        /// Lets the user choose a support action and agent, collects the eligible text files from
        /// the selection, and opens the Building Natural Language Outlines panel to generate them.
        /// </summary>
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

        /// <summary>
        /// Walks the current Solution Explorer selection recursively and returns every visible
        /// text file as a chat context item, the set of files that will receive outlines.
        /// </summary>
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
                    SelectedIdentifier.Create(
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
