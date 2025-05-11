using EnvDTE;
using EnvDTE80;
using FreeAIr.Helper;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using RunProcessAsTask;
using System.ComponentModel.Composition;
using System.Diagnostics;
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
                var w = new WaitGitWindow();
                await w.ShowDialogAsync();

                var commitMessage = w.GitDiffBody;
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
                                var commitMessage = answer.GetAnswer().CleanupFromQuotesAndThinks(
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
