namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.ResetMCPToolsCommandId)]
    public sealed class ResetMCPToolsCommand : BaseCommand<ResetMCPToolsCommand>
    {
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
