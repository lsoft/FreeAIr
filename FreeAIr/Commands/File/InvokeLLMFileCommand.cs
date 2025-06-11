using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Composer;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Linq;

namespace FreeAIr.Commands.File
{
    public abstract class InvokeLLMFileCommand<T> : BaseCommand<T>
        where T : InvokeLLMFileCommand<T>, new()
    {
        public InvokeLLMFileCommand(
            )
        {
        }

        protected abstract ChatKindEnum GetChatKind();

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!InternalPage.Instance.IsActiveAgentHasToken())
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    Resources.Resources.Code_NoToken
                    );
                return;
            }

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var chatContainer = componentModel.GetService<ChatContainer>();

            var sew = await VS.Windows.GetSolutionExplorerWindowAsync();
            var selections = (await sew.GetSelectionAsync()).ToList();
            if (selections.Count == 0)
            {
                return;
            }

            var kind = GetChatKind();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    kind,
                    null
                    ),
                null,
                FreeAIr.BLogic.ChatOptions.Default
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

                var contextItems = (await ContextComposer.ComposeFromFilePathAsync(
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

            chat.AddPrompt(
                UserPrompt.CreateContextItemBasedPrompt(
                    kind,
                    selections.ConvertAll(s => s.FullPath)
                    )
                );

            await ChatListToolWindow.ShowIfEnabledAsync();
        }

    }

}
