using FreeAIr.Agents;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Git.Parser;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.Windows;
using System.Collections.Generic;
using System.Windows;

namespace FreeAIr.Git
{
    public static class GitNaturalLanguageOutliner
    {
        public static void CollectOutlines(
            )
        {
            AgentsContextMenuCommandBridge.Show(
                GitDiffItemsCommandProcessor.Instance,
                InternalPage.Instance.GetAgentCollection()
                );
        }
    }


    public /*sealed*/ class GitDiffItemsCommandProcessor : CommandProcessor
    {
        public static readonly GitDiffItemsCommandProcessor Instance = new();

        protected override async System.Threading.Tasks.Task<List<SolutionItemChatContextItem>> CreateContextItemsAsync(
            )
        {
            var contextItems = new List<SolutionItemChatContextItem>();

            var backgroundTask = new GitCollectBackgroundTask(
                );
            var w = new WaitForTaskWindow(
                backgroundTask
                );
            await w.ShowDialogAsync();

            var gitDiffString = backgroundTask.Result;
            if (string.IsNullOrEmpty(gitDiffString))
            {
                await ShowErrorAsync("Cannot collect git patch. Please post outlines manually.");
                return contextItems;
            }

            var repositoryFolder = await GitRepositoryProvider.GetRepositoryFolderAsync();
            if (string.IsNullOrEmpty(repositoryFolder))
            {
                await ShowErrorAsync("Cannot determine git repository path. Please post outlines manually.");
                return contextItems;
            }

            var diff = new GitDiff(
                repositoryFolder,
                gitDiffString
                );

            foreach (var diffFile in diff.Files)
            {
                var contextItem = new SolutionItemChatContextItem(
                    new UI.Embedillo.Answer.Parser.SelectedIdentifier(
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

        private static async Task ShowErrorAsync(
            string error
            )
        {
            await VS.MessageBox.ShowErrorAsync(
                Resources.Resources.Error,
                error
                );
        }
    }

}
