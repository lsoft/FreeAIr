using FreeAIr.Helper;
using FreeAIr.Interaction;
using FreeAIr.Options2.Support;
using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Collections.Generic;
using FreeAIr.Chat;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.Git
{
    /// <summary>
    /// Drives the "Add natural language outlines to changed files" command: picks a support action
    /// and agent, then feeds the files touched by the pending git diff to the NL outlines panel so
    /// the AI can add the `&lt;summary&gt;` comments the NLO/RAG index relies on.
    /// </summary>
    public static class GitNaturalLanguageOutliner
    {
        /// <summary>
        /// Asks the user for a support action and agent, collects the files changed in the pending
        /// git diff, and opens the natural language outlines panel for them.
        /// </summary>
        public static async Task CollectOutlinesAsync(
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
                var chooser = componentModel.GetService<IUserChooser>();

                var chosenSupportAction = await chooser.ChooseSupportActionAsync(
                    Resources.Resources.Choose_support_action,
                    SupportScopeEnum.GenerateNaturalLanguageOutlines
                    );
                if (chosenSupportAction is null)
                {
                    return;
                }

                var chosenAgent = await chooser.ChooseAgentWithTokenAsync(
                    FreeAIr.Resources.Resources.Choose_agent_to_add_NL_outlines_to,
                    chosenSupportAction.AgentName
                    );
                if (chosenAgent is null)
                {
                    return;
                }

                var chosenSolutionItems = await CreateChosenSolutionItemsAsync();

                await componentModel.GetService<INaturalLanguageOutlinesPanel>().ShowAsync(
                    chosenSupportAction,
                    chosenAgent,
                    chosenSolutionItems
                    );
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Builds the chat context items for the natural language outlines panel: one per changed
        /// text file in the current git diff, each scoped to the diff's changed line ranges so only
        /// touched code is re-outlined. Binary and non-text files are skipped.
        /// </summary>
        private static async System.Threading.Tasks.Task<List<SolutionItemChatContextItem>> CreateChosenSolutionItemsAsync(
            )
        {
            var diff = await GitDiffCreator.BuildGitDiffAsync();
            if (diff is null)
            {
                return [];
            }

            var contextItems = new List<SolutionItemChatContextItem>();

            foreach (var diffFile in diff.Files)
            {
                if (string.IsNullOrEmpty(diffFile.NewFullPath))
                {
                    continue;
                }
                if (FileTypeHelper.GetFileType(diffFile.NewFullPath) != FileTypeEnum.Text)
                {
                    continue;
                }

                var contextItem = new SolutionItemChatContextItem(
                    SelectedIdentifier.Create(
                        diffFile.NewFullPath,
                        null
                        ),
                    false,
                    AddLineNumbersMode.RequiredForScopes(
                        diffFile.GetDiffChunks()
                        )
                    );
                contextItems.Add(contextItem);
            }

            return contextItems;
        }
    }

}
