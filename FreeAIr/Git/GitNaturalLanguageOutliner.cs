using FreeAIr.Options2.Agent;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Commands.File;
using FreeAIr.Git.Parser;
using FreeAIr.Helper;
using FreeAIr.NLOutline;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ViewModels;
using FreeAIr.UI.Windows;
using System.Collections.Generic;
using System.Windows;
using FreeAIr.Options2;

namespace FreeAIr.Git
{
    public static class GitNaturalLanguageOutliner
    {
        public static async Task CollectOutlinesAsync(
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var agentCollection = await FreeAIrOptions.DeserializeAgentCollectionAsync();

                var chosenAgent = await VisualStudioContextMenuCommandBridge.ShowAsync<AgentJson>(
                    "Choose agent to add NL outlines to changed files:",
                    agentCollection.Agents.ConvertAll(a => (a.Name, (object)a))
                    );
                if (chosenAgent is null)
                {
                    return;
                }

                var chosenSolutionItems = await CreateChosenSolutionItemsAsync();

                await NaturalLanguageOutlinesViewModel.ShowPanelAsync(chosenAgent, chosenSolutionItems);
            }
            catch (Exception excp)
            {
                //todo log
            }
        }

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
