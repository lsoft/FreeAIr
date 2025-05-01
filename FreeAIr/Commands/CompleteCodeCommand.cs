using EnvDTE;
using FreeAIr.BLogic.Tasks;
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
            var taskContainer = componentModel.GetService<AITaskContainer>();

            var (fileName, selectedCode) = await DocumentHelper.GetSelectedTextAsync();
            if (fileName is null || string.IsNullOrEmpty(selectedCode))
            {
                await VS.MessageBox.ShowWarningAsync(
                    "Error",
                    "Cannot obtain selected block of code"
                    );
                return;
            }

            var kind = AITaskKindEnum.CompleteCodeAccordingComments;

            taskContainer.StartTask(
                new TaskKind(
                    kind,
                    fileName
                    ),
                QueryBuilder.BuildQuery(kind, selectedCode)
                );

            if (ApiPage.Instance.SwitchToTaskWindow)
            {
                _ = await TaskListToolWindow.ShowAsync();
            }
        }

    }

}
