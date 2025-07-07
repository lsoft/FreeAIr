using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.BLogic.Context.Item;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;

namespace FreeAIr.Commands
{
    [Command(PackageIds.StartDiscussionCommandId)]
    internal sealed class StartDiscussionCommand : BaseCommand<StartDiscussionCommand>
    {
        public StartDiscussionCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

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

            var kind = ChatKindEnum.Discussion;

            var chat = await chatContainer.StartChatAsync(
                new ChatDescription(
                    kind,
                    null
                    ),
                null,
                await FreeAIr.BLogic.ChatOptions.GetDefaultAsync()
                );
            if (chat is null)
            {
                return;
            }

            chat.ChatContext.AddItem(
                new SolutionItemChatContextItem(
                    std.CreateSelectedIdentifier(),
                    false,
                    AddLineNumbersMode.NotRequired
                    )
                );

            _ = await ChatListToolWindow.ShowAsync();
        }

    }
}
