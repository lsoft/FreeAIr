using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Composer;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;

namespace FreeAIr.Commands
{
    internal abstract class InvokeLLMCommand<T> : BaseCommand<T>
        where T : InvokeLLMCommand<T>, new()
    {
        protected InvokeLLMCommand(
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

            var std = await TextDescriptorHelper.GetSelectedTextAsync();
            if (std is null || std.SelectedSpan is null)
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    Resources.Resources.Code_NoSelectedCode
                    );
                return;
            }

            var contextItems = (await CSharpContextComposer.ComposeFromActiveDocumentAsync(
                )).ConvertToChatContextItem();

            var kind = GetChatKind();

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    kind,
                    std
                    ),
                null,
                FreeAIr.BLogic.ChatOptions.Default
                );
            if (chat is null)
            {
                return;
            }

            chat.ChatContext.AddItems(
                contextItems
                );

            chat.AddPrompt(
                UserPrompt.CreateCodeBasedPrompt(
                    kind,
                    std.FileName,
                    std.FilePath + std.SelectedSpan.ToString()
                    )
                );

            await ChatListToolWindow.ShowIfEnabledAsync();
        }

    }
}
