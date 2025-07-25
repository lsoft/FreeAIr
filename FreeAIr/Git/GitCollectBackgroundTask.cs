using FreeAIr.Git;
using FreeAIr.UI.Windows;

namespace FreeAIr.BLogic
{
    public sealed class GitCollectBackgroundTask : BackgroundTask
    {
        public override string TaskDescription => FreeAIr.Resources.Resources.Please_wait_for_git_patch_building;

        public string? Result
        {
            get;
            private set;
        }

        public GitCollectBackgroundTask()
        {
            StartAsyncTask();
        }

        protected override async Task RunWorkingTaskAsync(
            )
        {
            //in case of exception set it null first
            Result = null;

            Result = await GitDiffCollector.CombineDiffAsync(_cancellationTokenSource.Token);
        }
    }
}
