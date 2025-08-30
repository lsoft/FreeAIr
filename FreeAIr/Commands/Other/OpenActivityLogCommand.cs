using System.Diagnostics;

namespace FreeAIr.Commands.Other
{

    [Command(PackageIds.OpenActivityLogCommandId)]
    public sealed class OpenActivityLogCommand : BaseCommand<OpenActivityLogCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                var filePath = ActivityLog.LogFilePath;
                if (!System.IO.File.Exists(filePath))
                {
                    return;
                }

                //var folderPath = new FileInfo(filePath).Directory.FullName;
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("FreeAIr", $"Ошибка получения пути к ActivityLog: {ex.Message}");
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
