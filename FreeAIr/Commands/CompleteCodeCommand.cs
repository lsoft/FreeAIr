using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;

namespace FreeAIr.Commands
{
    [Command(PackageIds.FreeAIrCompleteCodeCommandId)]
    internal sealed class CompleteCodeCommand : BaseCommand<CompleteCodeCommand>
    {
        public CompleteCodeCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var taskContainer = componentModel.GetService<ChatContainer>();

            var (fileName, selectedCode) = await DocumentHelper.GetSelectedTextAsync();
            if (fileName is null || string.IsNullOrEmpty(selectedCode))
            {
                await VS.MessageBox.ShowWarningAsync(
                    "Error",
                    "Cannot obtain selected block of code"
                    );
                return;
            }

            var kind = ChatKindEnum.CompleteCodeAccordingComments;

            taskContainer.StartChat(
                new ChatDescription(
                    kind,
                    fileName
                    ),
                UserPrompt.CreateCodeBasedPrompt(kind, fileName, selectedCode)
                );

            if (ResponsePage.Instance.SwitchToTaskWindow)
            {
                _ = await ChatListToolWindow.ShowAsync();
            }
        }

    }

}
