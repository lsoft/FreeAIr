using FreeAIr.Helper;
using FreeAIr.UI.Windows;

namespace FreeAIr.MCP.McpServerProxy.Github
{
    /// <summary>Runs <see cref="GithubMcpServerProxy.InstallAsync"/> off the UI thread with the standard progress-bar treatment, so downloading the GitHub server executable does not freeze Visual Studio.</summary>
    public sealed class GithubMCPInstallBackgroundTask : BackgroundTask
    {
        /// <summary>The label shown in the progress UI while the GitHub server is being installed.</summary>
        public override string TaskDescription => "Installing GitHub.com MCP server...";

        /// <summary>Whether the install finished without throwing.</summary>
        public bool SuccessfullyInstalled
        {
            get;
            private set;
        }

        /// <summary>Creates the task and immediately starts the background install.</summary>
        public GithubMCPInstallBackgroundTask(
            )
        {
            StartAsyncTask();
        }

        /// <summary>Installs the server and records whether it succeeded; rethrows so the background-task infrastructure still surfaces the failure.</summary>
        protected override async Task RunWorkingTaskAsync()
        {
            try
            {
                SuccessfullyInstalled = false;

                await GithubMcpServerProxy.Instance.InstallAsync();

                SuccessfullyInstalled = true;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
                throw;
            }

            return;
        }

    }
}
