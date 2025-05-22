using FreeAIr.MCP.Agent;
using FreeAIr.MCP.Agent.Github;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.Windows;
using System.Windows;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.InstallGithubMCPServerCommandId)]
    internal sealed class InstallGithubMCPServerCommand : BaseCommand<InstallGithubMCPServerCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            var backgroundTask = new GithubMCPInstallBackgroundTask(
                );
            var w = new WaitForTaskWindow(
                backgroundTask
                );
            await w.ShowDialogAsync();

            var installResult = backgroundTask.SuccessfullyInstalled;
            if (installResult)
            {
                await AgentCollection.ProcessAgentAsync(GithubAgent.Instance);

                await VS.MessageBox.ShowAsync(
                    string.Empty,
                    $"GitHub MCP server installed SUCCESSFULLY.",
                    buttons: Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK
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
    }
}
