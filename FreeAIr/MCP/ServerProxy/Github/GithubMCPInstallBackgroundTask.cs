using FreeAIr.Helper;
using FreeAIr.UI.Windows;

namespace FreeAIr.MCP.McpServerProxy.Github
{
    public sealed class GithubMCPInstallBackgroundTask : BackgroundTask
    {
        public override string TaskDescription => "Installing GitHub.com MCP server...";

        public bool SuccessfullyInstalled
        {
            get;
            private set;
        }

        public GithubMCPInstallBackgroundTask(
            )
        {
            StartAsyncTask();
        }

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
