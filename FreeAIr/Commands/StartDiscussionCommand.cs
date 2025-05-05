using EnvDTE;
using FreeAIr.BLogic;
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

            var kind = ChatKindEnum.Discussion;

            chatContainer.StartChat(
                new ChatDescription(
                    kind,
                    null
                    ),
                null
                );

            _ = await ChatListToolWindow.ShowAsync();
        }

    }
}
