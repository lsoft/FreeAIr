using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands.Other
{
    /// <summary>
    /// The menu command that opens the RAG Calibration tool window, used to tune and evaluate the
    /// embedding-based natural language search over the NLO index.
    /// </summary>
    [Command(PackageIds.OpenRagCalibrationWindowCommandId)]
    public sealed class OpenRagCalibrationWindowCommand : BaseCommand<OpenRagCalibrationWindowCommand>
    {
        /// <summary>
        /// Creates the command instance; no setup is required beyond the base VS command wiring.
        /// </summary>
        public OpenRagCalibrationWindowCommand(
            )
        {
        }

        /// <summary>
        /// Shows the RAG Calibration tool window when the user invokes the command.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await RagCalibrationToolWindow.ShowPaneAsync();
        }
    }
}
