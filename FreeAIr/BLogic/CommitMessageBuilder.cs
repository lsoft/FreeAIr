using EnvDTE;
using EnvDTE80;
using FreeAIr.Git;
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

                var gitDiff = backgroundTask.Result;
                if (string.IsNullOrEmpty(gitDiff))
                {
                    await ShowErrorAsync("Cannot collect git patch. Please enter commit message manually.");
                    return;
                }

                var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
                var chatContainer = componentModel.GetService<ChatContainer>();

                var chat = await chatContainer.StartChatAsync(
                    new ChatDescription(
                        ChatKindEnum.GenerateCommitMessage,
                        null
                        ),
                    UserPrompt.CreateCommitMessagePrompt(gitDiff),
                    ChatOptions.NoToolAutoProcessedTextResponse
                    );

                if (chat is not null)
                {
                    var commitMessage = await chat.WaitForPromptCleanAnswerAsync(
                        Environment.NewLine
                        );
                    if (!string.IsNullOrEmpty(commitMessage))
                    {
                        CommitMessageTextBox.Text = commitMessage;
                        return;
                    }
                }

                ShowErrorAsync("Cannot receive AI answer. Please enter commit message manually.")
                    .FileAndForget(nameof(ShowErrorAsync));

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
                //in case of exception set it null first
                Result = null;

                Result = await GitDiffCombiner.CombineDiffAsync(_cancellationTokenSource.Token);
            }
        }


    }
}
