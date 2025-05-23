using EnvDTE;
using EnvDTE80;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;
using RunProcessAsTask;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.BLogic
{
    [Export(typeof(CommitMessageBuilder))]
    public sealed class CommitMessageBuilder
    {
        private readonly DTEEvents _dteEvents;

        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public TextBox? CommitMessageTextBox
        {
            get;
            private set;
        }
        public Button? BuildCommitMessageButton
        {
            get;
            private set;
        }

        public bool IsEnabled => 
            CommitMessageTextBox is not null
            && BuildCommitMessageButton is not null
            ;

        [ImportingConstructor]
        public CommitMessageBuilder()
        {
            var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
            _dteEvents = ((Events2)dte.Events).DTEEvents;
            _dteEvents.OnBeginShutdown += DTEEvents_OnBeginShutdown;
        }

        private void DTEEvents_OnBeginShutdown()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task RunAsync()
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var cancellationToken = _cancellationTokenSource.Token;

                while (true)
                {
                    try
                    {
                        foreach (System.Windows.Window w in Application.Current.Windows)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            var cTextBox = w.GetRecursiveByName<TextBox>("textBox");
                            if (cTextBox is null)
                            {
                                continue;
                            }

                            var dcButton = w.GetRecursiveByName<Button>("describeChangesButton");
                            if (dcButton is null)
                            {
                                continue;
                            }

                            var vsButtonPanel = VisualTreeHelper.GetParent(dcButton);
                            if (vsButtonPanel is not StackPanel vsPanel)
                            {
                                continue;
                            }

                            var buildCommitMessageButton = new Button
                            {
                                Content = new CrispImage
                                {
                                    Moniker = KnownMonikers.GitRepository
                                },
                                HorizontalAlignment = HorizontalAlignment.Center,
                                HorizontalContentAlignment = HorizontalAlignment.Center,
                                Margin = new System.Windows.Thickness(0),
                                Style = dcButton.Style,
                                ToolTip = "FreeAIr support: generate commit message"
                            };

                            buildCommitMessageButton.Click += BuildCommitMessageButton_Click;

                            vsPanel.Children.Insert(0, buildCommitMessageButton);

                            CommitMessageTextBox = cTextBox;
                            BuildCommitMessageButton = buildCommitMessageButton;
                            return;
                        }

                        await Task.Delay(5_000, cancellationToken);
                    }
                    catch (Exception excp)
                    {
                        //todo

                        //not to ddos VS UI
                        await Task.Delay(15_000, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //this is ok
            }
            catch (Exception excp)
            {
                //todo
            }
        }

        private async void BuildCommitMessageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var backgroundTask = new GitCollectBackgroundTask(
                        );
                var w = new WaitForTaskWindow(
                    backgroundTask
                    );
                await w.ShowDialogAsync();

                var commitMessage = backgroundTask.Result;
                if (string.IsNullOrEmpty(commitMessage))
                {
                    await ShowErrorAsync("Cannot collect git patch. Please enter commit message manually.");
                    return;
                }

                var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
                var chatContainer = componentModel.GetService<ChatContainer>();

                _ = await chatContainer.StartChatAsync(
                    new ChatDescription(
                        ChatKindEnum.GenerateCommitMessage,
                        null
                        ),
                    UserPrompt.CreateCommitMessagePrompt(commitMessage),
                    (chat, answer) =>
                    {
                        //prompt has answered
                        if (chat is not null && chat.Status == ChatStatusEnum.Ready)
                        {
                            if (answer is not null)
                            {
                                var commitMessage = answer.GetTextualAnswer().CleanupFromQuotesAndThinks(
                                    Environment.NewLine
                                    );
                                if (!string.IsNullOrEmpty(commitMessage))
                                {
                                    ThreadHelper.JoinableTaskFactory.RunAsync(
                                        async () =>
                                        {
                                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                                            CommitMessageTextBox.Text = commitMessage;
                                        }).FileAndForget("CommitMessageTextBox.Text");
                                    return;
                                }
                            }
                        }

                        ShowErrorAsync("Cannot receive AI answer. Please enter commit message manually.")
                            .FileAndForget(nameof(ShowErrorAsync));
                    }
                    );

                await ChatListToolWindow.ShowIfEnabledAsync();
            }
            catch (Exception excp)
            {
                //todo log
            }
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

        private sealed class GitCollectBackgroundTask : BackgroundTask
        {
            public override string TaskDescription => "Please wait for git patch building...";

            public string? Result
            {
                get;
                private set;
            }

            protected override async Task RunWorkingTaskAsync(
                )
            {
                Result = null;

                await Task.Delay(10000);

                var cancellationToken = _cancellationTokenSource.Token;

                var gitExt = (IGitExt)await FreeAIrPackage.Instance.GetServiceAsync(typeof(IGitExt));
                if (gitExt is null)
                {
                    //todo log
                    return;
                }
                if (gitExt.ActiveRepositories.Count != 1)
                {
                    //todo log
                    return;
                }

                var activeRepository = gitExt.ActiveRepositories[0] as IGitRepositoryInfo2;
                if (activeRepository.Remotes.Count != 1)
                {
                    //todo log
                    return;
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
                    return;
                }
                summaryDiff.AppendLine(diffIndex);

                var lsFiles = await ProcessHelper.RunSilentlyAsync(
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

                Result = summaryDiff.ToString();
            }
        }


    }

    public static class GitDiffProcess
    {
        public static async Task<string?> RunAndParseGitDiffSilentlyAsync(
            string workingDirectory,
            string? nonVersionedFile,
            CancellationToken cancellationToken
            )
        {
            ProcessResults diff;
            //int skipLineCount;
            if (string.IsNullOrEmpty(nonVersionedFile))
            {
                diff = await ProcessHelper.RunWithTimeoutAsync(
                    new ProcessStartInfo
                    {
                        WorkingDirectory = workingDirectory,
                        FileName = "git.exe",
                        Arguments = $"diff",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    },
                    cancellationToken
                    );

                //skipLineCount = 3;
            }
            else
            {
                diff = await ProcessHelper.RunWithTimeoutAsync(
                    new ProcessStartInfo
                    {
                        WorkingDirectory = workingDirectory,
                        FileName = "git.exe",
                        Arguments = $"diff --no-index /dev/null {nonVersionedFile}",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    },
                    cancellationToken
                    );

                //skipLineCount = 4;
            }

            if (diff.ExitCode != 0 && diff.ExitCode != 1)
            {
                return null;
            }
            if (diff.StandardError.Length > 0)
            {
                return null;
            }
            if (diff.StandardOutput.Length < 5)
            {
                return null;
            }

            //var patch = diff.StandardOutput.Skip(skipLineCount).ToList();
            //if (patch.Count > 0)
            //{
            //    var lastLine = patch[patch.Count - 1];
            //    if (lastLine.Contains(@"\ No newline at end of file"))
            //    {
            //        patch.RemoveAt(patch.Count - 1);
            //    }
            //}
            //patch[0] = patch[0].Replace("+++ b/", "+++ ");

            return string.Join(
                Environment.NewLine,
                diff.StandardOutput
                );
        }
    }
}
