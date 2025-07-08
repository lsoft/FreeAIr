using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Composer;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Linq;

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

            var sew = await VS.Windows.GetSolutionExplorerWindowAsync();
            var selections = (await sew.GetSelectionAsync()).ToList();
            if (selections.Count == 0)
            {
                return;
            }

            var chosenSupport = await SupportContextMenu.ChooseSupportAsync(
                "Choose support:",
                SupportScopeEnum.FileInSolutionTree
                );
            if (chosenSupport is null)
            {
                return;
            }

            var supportContext = await SupportContext.WithSelectedFilesAsync(
                selections
                );

            var chosenAgent = await AgentContextMenu.ChooseAgentWithTokenAsync(
                "Choose agent:",
                chosenSupport.AgentName
                );
            if (chosenAgent is null)
            {
                return;
            }

            var kind = ChatKindEnum.Discussion;

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    kind,
                    null
                    ),
                null,
                await FreeAIr.BLogic.ChatOptions.GetDefaultAsync(chosenAgent)
                );
            if (chat is null)
            {
                return;
            }

            foreach (var selection in selections)
            {
                if (selection.Type != SolutionItemType.PhysicalFile)
                {
                    continue;
                }

                var contextItems = (await CSharpContextComposer.ComposeFromFilePathAsync(
                    selection.FullPath
                    )).ConvertToChatContextItem();

                chat.ChatContext.AddItem(
                    new SolutionItemChatContextItem(
                        new UI.Embedillo.Answer.Parser.SelectedIdentifier(
                            selection.FullPath,
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
                chosenSupport.Prompt
                );

            chat.AddPrompt(
                UserPrompt.CreateTextBasedPrompt(
                    promptText
                    )
                );

            await ChatListToolWindow.ShowIfEnabledAsync();
        }
    }
}
