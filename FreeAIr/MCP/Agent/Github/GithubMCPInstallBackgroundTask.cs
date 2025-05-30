using FreeAIr.UI.Windows;

namespace FreeAIr.MCP.Agent.Github
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
        }

        protected override async Task RunWorkingTaskAsync()
        {
            try
            {
                SuccessfullyInstalled = false;

                await GithubAgent.Instance.InstallAsync();

                SuccessfullyInstalled = true;
            }
            catch (Exception excp)
            {
                //todo log
                throw;
            }

            return;
        }

    }
}
