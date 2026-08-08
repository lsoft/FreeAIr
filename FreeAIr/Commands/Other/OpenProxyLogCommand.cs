using FreeAIr.MCP.McpServerProxy;
using System.Diagnostics;

namespace FreeAIr.Commands.Other
{
    /// <summary>
    /// The menu command that opens Windows Explorer at the MCP server proxy's unpacked folder,
    /// where its log files live, for diagnosing the MCP proxy process.
    /// </summary>
    [Command(PackageIds.OpenProxyLogCommandId)]
    internal sealed class OpenProxyLogCommand : BaseCommand<OpenProxyLogCommand>
    {
        /// <summary>
        /// Opens Explorer at the proxy's unpacked folder, or logs an error if that fails.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            try
            {
                if (!System.IO.Directory.Exists(McpServerProxyApplication.ProxyUnpackedFolderPath))
                {
                    return;
                }

                Process.Start(
                    "explorer.exe",
                    McpServerProxyApplication.ProxyUnpackedFolderPath
                    );
            }
            catch (Exception ex)
            {
                ActivityLog.LogError("FreeAIr", $"Ошибка получения пути к proxy Log: {ex.Message}");
            }
        }

        /// <summary>
        /// Enables the command only when the proxy's unpacked folder currently exists on disk.
        /// </summary>
        protected override void BeforeQueryStatus(EventArgs e)
        {
            if (!System.IO.Directory.Exists(McpServerProxyApplication.ProxyUnpackedFolderPath))
            {
                this.Command.Enabled = false;
                return;
            }

            this.Command.Enabled = true;
        }

    }

}
