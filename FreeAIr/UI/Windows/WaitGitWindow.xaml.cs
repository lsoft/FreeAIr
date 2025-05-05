using FreeAIr.BLogic;
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FreeAIr.UI.Windows
{
    public partial class WaitGitWindow : Window
    {
        private CancellationTokenSource? _cancellationTokenSource = new();
        
        private Task<string>? _workingTask;

        public string? GitDiffBody
        {
            get;
            private set;
        }

        public WaitGitWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //todo log

            CloseWindowAsync()
                .FileAndForget(nameof(CloseWindowAsync));
        }

        private async Task CloseWindowAsync()
        {
            if (_workingTask is not null && !_workingTask.IsCompleted)
            {
                _cancellationTokenSource.Cancel();

                await _workingTask;
            }

            _cancellationTokenSource.Dispose();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _workingTask = CollectGitDiffAsync(
                    _cancellationTokenSource.Token
                    );

                GitDiffBody = await _workingTask;
            }
            catch (OperationCanceledException excp)
            {
                //nothing to do, this is ok
                this.GitDiffBody = null;
            }
            catch (Exception excp)
            {
                //todo log
                this.GitDiffBody = null;
            }

            this.Close();
        }

        private static async Task<string?> CollectGitDiffAsync(
            CancellationToken cancellationToken
            )
        {
            var gitExt = (IGitExt)await FreeAIrPackage.Instance.GetServiceAsync(typeof(IGitExt));
            if (gitExt is null)
            {
                //todo log
                return null;
            }
            if (gitExt.ActiveRepositories.Count != 1)
            {
                //todo log
                return null;
            }

            var activeRepository = gitExt.ActiveRepositories[0] as IGitRepositoryInfo2;
            if (activeRepository.Remotes.Count != 1)
            {
                //todo log
                return null;
            }

            var repositoryFolder = activeRepository.RepositoryPath;

            var summaryDiff = new StringBuilder();

            var diffIndex = await GitDiffProcess.RunAndParseGitDiffSilentlyAsync(
                repositoryFolder,
                string.Empty,
                cancellationToken
                );
            if (string.IsNullOrEmpty(diffIndex))
            {
                //todo log
                return null;
            }
            summaryDiff.AppendLine(diffIndex);

            var lsFiles = await ProcessProcess.RunSilentlyAsync(
                repositoryFolder,
                "git.exe",
                @"ls-files --others --exclude-standard",
                cancellationToken
                );

            foreach (var lsFile in lsFiles.StandardOutput)
            {
                var diffNoIndex = await GitDiffProcess.RunAndParseGitDiffSilentlyAsync(
                    repositoryFolder,
                    lsFile,
                    cancellationToken
                    );

                summaryDiff.AppendLine(diffNoIndex);
            }

            return summaryDiff.ToString();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
