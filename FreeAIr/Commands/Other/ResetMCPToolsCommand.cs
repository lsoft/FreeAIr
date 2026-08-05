namespace FreeAIr.Commands.Other
{
    /// <summary>
    /// The menu command that clears the recorded execution status of MCP tools (whether each has
    /// been run and confirmed) so the user is prompted for approval again on next use.
    /// </summary>
    [Command(PackageIds.ResetMCPToolsCommandId)]
    public sealed class ResetMCPToolsCommand : BaseCommand<ResetMCPToolsCommand>
    {
        /// <summary>
        /// Resets the MCP tools' recorded execution status and reports success, or logs an error
        /// if the reset fails.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                InternalPage.Instance.ResetMCPToolsExecutionStatus();

                await VS.MessageBox.ShowAsync("Successfully completed");
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("FreeAIr", $"Error during MCP tools status resetting: {ex.Message}");
            }
        }

        /// <summary>
        /// Enables the command only when the ActivityLog file currently exists on disk.
        /// </summary>
        protected override void BeforeQueryStatus(EventArgs e)
        {
            var filePath = ActivityLog.LogFilePath;
            if (!System.IO.File.Exists(filePath))
            {
                this.Command.Enabled = false;
                return;
            }

            this.Command.Enabled = true;
        }

    }

}
