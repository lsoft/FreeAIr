using FreeAIr.BLogic;
using FreeAIr.Git.Parser;
using FreeAIr.UI.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.Git
{
    public static class GitDiffCreator
    {
        public static async System.Threading.Tasks.Task<GitDiff> BuildGitDiffAsync(
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var backgroundTask = new GitCollectBackgroundTask(
                );
            var w = new WaitForTaskWindow(
                backgroundTask
                );
            await w.ShowDialogAsync();

            var gitDiffString = backgroundTask.Result;
            if (string.IsNullOrEmpty(gitDiffString))
            {
                await ShowErrorAsync("Cannot collect git patch.");
                return null;
            }

            var repositoryFolder = await GitRepositoryProvider.GetRepositoryFolderAsync();
            if (string.IsNullOrEmpty(repositoryFolder))
            {
                await ShowErrorAsync("Cannot determine git repository path.");
                return null;
            }

            var diff = new GitDiff(
                repositoryFolder,
                gitDiffString
                );
            return diff;
        }

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
