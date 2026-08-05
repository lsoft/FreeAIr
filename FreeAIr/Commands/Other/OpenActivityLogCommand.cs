using System.Diagnostics;

namespace FreeAIr.Commands.Other
{

    /// <summary>
    /// The menu command that opens Windows Explorer with the Visual Studio ActivityLog.xml file
    /// selected, for inspecting VS's own diagnostic log.
    /// </summary>
    [Command(PackageIds.OpenActivityLogCommandId)]
    public sealed class OpenActivityLogCommand : BaseCommand<OpenActivityLogCommand>
    {
        /// <summary>
        /// Opens Explorer with the ActivityLog file selected, or logs an error if that fails.
        /// </summary>
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
