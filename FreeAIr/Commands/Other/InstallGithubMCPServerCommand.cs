using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.InstallGithubMCPServerCommandId)]
    internal sealed class InstallGithubMCPServerCommand : BaseCommand<InstallGithubMCPServerCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var installResult = await MCP.Github.Installer.InstallAsync();
            if (installResult)
            {
                await VS.MessageBox.ShowAsync(
                    string.Empty,
                    $"GitHub MCP server installed SUCCESSFULLY."
                    );
            }
            else
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    $"Installation GitHub MCP server fails. Please install it manually."
                    );
            }
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            this.Command.Enabled = !MCP.Github.Installer.IsInstalled();
        }
    }
}
