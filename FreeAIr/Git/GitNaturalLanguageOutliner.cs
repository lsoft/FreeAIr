using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ViewModels;
using System.Collections.Generic;

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

                var chosenSupportAction = await SupportContextMenu.ChooseSupportAsync(
                    Resources.Resources.Choose_support_action,
                    SupportScopeEnum.GenerateNaturalLanguageOutlines
                    );
                if (chosenSupportAction is null)
                {
                    return;
                }

                var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                    FreeAIr.Resources.Resources.Choose_agent_to_add_NL_outlines_to,
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
    }

}
