using FreeAIr.Git;
using FreeAIr.UI.Windows;

namespace FreeAIr.BLogic
{
    /// <summary>
    /// Runs <see cref="GitDiffCollector.CombineDiffAsync"/> off the UI thread while a
    /// <see cref="WaitForTaskWindow"/> keeps the user informed, so building the commit message diff
    /// never freezes Visual Studio.
    /// </summary>
    public sealed class GitCollectBackgroundTask : BackgroundTask
    {
        /// <summary>
        /// The text shown in the wait dialog while the diff is being collected.
        /// </summary>
        public override string TaskDescription => FreeAIr.Resources.Resources.Please_wait_for_git_patch_building;

        /// <summary>
        /// The combined git diff once collection finishes, or null if it failed or found nothing to
        /// diff.
        /// </summary>
        public string? Result
        {
            get;
            private set;
        }

        /// <summary>Creates the task and immediately starts collecting the diff in the background.</summary>
        public GitCollectBackgroundTask()
        {
            StartAsyncTask();
        }

        /// <summary>
        /// Collects the combined diff of the current repository and stores it in <see cref="Result"/>.
        /// </summary>
        protected override async Task RunWorkingTaskAsync(
            )
        {
            //in case of exception set it null first
            Result = null;

            Result = await GitDiffCollector.CombineDiffAsync(_cancellationTokenSource.Token);
        }
    }
}
