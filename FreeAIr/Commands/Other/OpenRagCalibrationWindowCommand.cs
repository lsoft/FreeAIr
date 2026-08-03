using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.OpenRagCalibrationWindowCommandId)]
    public sealed class OpenRagCalibrationWindowCommand : BaseCommand<OpenRagCalibrationWindowCommand>
    {
        public OpenRagCalibrationWindowCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await RagCalibrationToolWindow.ShowPaneAsync();
        }
    }
}
