using EnvDTE;
using FreeAIr.BLogic.Tasks;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio.ComponentModelHost;

namespace FreeAIr.Commands
{
    [Command(PackageIds.FreeAIrOptimizeCommandId)]
    internal sealed class OptimizeCodeCommand : BaseCommand<OptimizeCodeCommand>
    {
        public OptimizeCodeCommand(
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

            var kind = AITaskKindEnum.OptimizeCode;

            taskContainer.StartTask(
                new TaskKind(
                    kind,
                    fileName
                    ),
                QueryBuilder.BuildQuery(kind, selectedCode)
                );

            if (ResponsePage.Instance.SwitchToTaskWindow)
            {
                _ = await TaskListToolWindow.ShowAsync();
            }
        }

    }

}
