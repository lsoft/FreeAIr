using FreeAIr.BLogic;
using FreeAIr.Git.Parser;
using FreeAIr.Interaction;
using Microsoft.VisualStudio.ComponentModelHost;

namespace FreeAIr.Git
{
    /// <summary>
    /// Turns the pending changes of the current git repository into a parsed <see cref="GitDiff"/>,
    /// combining the raw diff collected by <see cref="GitCollectBackgroundTask"/> with the
    /// repository folder resolved by <see cref="GitRepositoryProvider"/>.
    /// </summary>
    public static class GitDiffCreator
    {
        /// <summary>
        /// Collects the pending diff in a wait dialog and parses it into a <see cref="GitDiff"/>.
        /// Shows an error and returns null when there is nothing to diff or the repository folder
        /// cannot be determined.
        /// </summary>
        public static async System.Threading.Tasks.Task<GitDiff> BuildGitDiffAsync(
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));

            var backgroundTask = new GitCollectBackgroundTask(
                );
            await componentModel.GetService<IBackgroundTaskShower>().ShowAsync(
                backgroundTask
                );

            var gitDiffString = backgroundTask.Result;
            if (string.IsNullOrEmpty(gitDiffString))
            {
                await ShowErrorAsync(FreeAIr.Resources.Resources.Cannot_collect_git_patch);
                return null;
            }

            var repositoryFolder = await GitRepositoryProvider.GetRepositoryFolderAsync();
            if (string.IsNullOrEmpty(repositoryFolder))
            {
                await ShowErrorAsync(FreeAIr.Resources.Resources.Cannot_determine_git_repository_path);
                return null;
            }

            var diff = new GitDiff(
                repositoryFolder,
                gitDiffString
                );
            return diff;
        }

        /// <summary>
        /// Shows a modal error message box with the given text.
        /// </summary>
        private static async Task ShowErrorAsync(
            string error
            )
        {
            await VS.MessageBox.ShowErrorAsync(
                Resources.Resources.Error,
                error
                );
        }

    }
}
