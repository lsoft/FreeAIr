using FreeAIr.MCP.McpServerProxy;
using System.Diagnostics;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.OpenProxyLogCommandId)]
    internal sealed class OpenProxyLogCommand : BaseCommand<OpenProxyLogCommand>
    {
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
