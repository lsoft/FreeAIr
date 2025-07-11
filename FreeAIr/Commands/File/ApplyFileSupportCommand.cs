using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Composer;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FreeAIr.Commands.File
{
    [Command(PackageIds.ApplyFileSupportCommandId)]
    internal sealed class ApplyFileSupportCommand : BaseCommand<ApplyFileSupportCommand>
    {
        public ApplyFileSupportCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var allSelectedFiles = await GetSelectedFilesAsync();

            var chosenSupportAction = await SupportContextMenu.ChooseSupportAsync(
                "Choose support action:",
                SupportScopeEnum.FileInSolutionTree
                );
            if (chosenSupportAction is null)
            {
                return;
            }

            var supportContext = await SupportContext.WithSolutionItemsAsync(
                allSelectedFiles
                );

            var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                "Choose agent:",
                chosenSupportAction.AgentName
                );
            if (chosenAgent is null)
            {
                return;
            }

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    null
                    ),
                null,
                await FreeAIr.BLogic.ChatOptions.GetDefaultAsync(chosenAgent)
                );
            if (chat is null)
            {
                return;
            }

            foreach (var selectedFile in allSelectedFiles)
            {
                if (selectedFile.Type != SolutionItemType.PhysicalFile)
                {
                    continue;
                }

                var contextItems = (await CSharpContextComposer.ComposeFromFilePathAsync(
                    selectedFile.FullPath
                    )).ConvertToChatContextItem();

                chat.ChatContext.AddItem(
                    new SolutionItemChatContextItem(
                        new UI.Embedillo.Answer.Parser.SelectedIdentifier(
                            selectedFile.FullPath,
                            null
                            ),
                        false,
                        AddLineNumbersMode.NotRequired
                        )
                    );

                chat.ChatContext.AddItems(
                    contextItems
                    );
            }

            var promptText = supportContext.ApplyVariablesToPrompt(
                chosenSupportAction.Prompt
                );

            chat.AddPrompt(
                UserPrompt.CreateTextBasedPrompt(
                    promptText
                    )
                );

            await ChatListToolWindow.ShowIfEnabledAsync();
        }

        private static async System.Threading.Tasks.Task<List<SolutionItem>> GetSelectedFilesAsync()
        {
            var sew = await VS.Windows.GetSolutionExplorerWindowAsync();
            var selections = await sew.GetSelectionAsync();

            var allChildren = new List<SolutionItem>();
            foreach (var selection in selections)
            {
                var children = await selection.ProcessDownRecursivelyForAsync(
                    item =>
                        !item.IsNonVisibleItem
                        && item.Type == SolutionItemType.PhysicalFile
                        && item.FullPath.GetFileType() == FileTypeEnum.Text
                        ,
                    false,
                    CancellationToken.None
                    );
                allChildren.AddRange(children.Select(c => c.SolutionItem));
            }

            return allChildren;
        }
    }
}
